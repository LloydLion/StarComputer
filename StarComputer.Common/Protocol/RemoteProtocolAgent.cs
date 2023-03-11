using Microsoft.Extensions.Logging;
using StarComputer.Common.Abstractions;
using StarComputer.Common.Abstractions.Protocol;
using StarComputer.Common.Abstractions.Protocol.Bodies;
using StarComputer.Common.Abstractions.Threading;
using StarComputer.Common.Abstractions.Utils;
using StarComputer.Common.Threading;
using System.Net;
using System.Net.Sockets;

namespace StarComputer.Common.Protocol
{
	public class RemoteProtocolAgent : IRemoteProtocolAgent
	{
		private static readonly EventId MessageSendErrorID = new(91, "MessageSendError");
		private static readonly EventId AgentDisconnectedID = new(92, "AgentDisconnected");
		private static readonly EventId AgentReconnectedID = new(93, "AgentReconnected");
		private static readonly EventId AgentConnectionLostID = new(94, "AgentConnectionLost");
		private static readonly EventId NewMessageDispatchedID = new(95, "NewMessageDispatched");
		private static readonly EventId NewMessageWritenID = new(96, "NewMessageWriten");
		private static readonly EventId NewMessageHandledID = new(97, "NewMessageHandled");
		private static readonly EventId MessageHandleErrorID = new(98, "MessageHandleError");


		private SocketClient client;
		private readonly IRemoteAgentWorker agentWorker;
		private readonly ILogger logger;
		private readonly IBodyTypeResolver bodyTypeResolver;
		private readonly string name;

		private Thread workThread;
		private IThreadDispatcher<WriteThreadTask> workThreadDispatcher;


		public IPEndPoint CurrentEndPoint => client.EndPoint;

		public Guid UniqueAgentId { get; } = Guid.NewGuid();


		public RemoteProtocolAgent(TcpClient client, IRemoteAgentWorker agentWorker, ILogger logger, IBodyTypeResolver bodyTypeResolver, string name = "#")
		{
			this.client = new(client, logger, "RPA: " + name);

			workThread = new Thread(WorkThreadHandler);
			workThreadDispatcher = new ThreadDispatcher<WriteThreadTask>(workThread, WriteMessage);

			this.agentWorker = agentWorker;
			this.logger = logger;
			this.bodyTypeResolver = bodyTypeResolver;
			this.name = name;
		}


		public void Start()
		{
			workThread.Start();
		}

		public void Disconnect()
		{
			foreach (var item in workThreadDispatcher.GetQueue())
				item.Task.SetException(new Exception("Agent was closed"));
			workThreadDispatcher.Close();

			agentWorker.HandleDisconnect(this);

			if (workThread.IsAlive && workThread.ManagedThreadId != Environment.CurrentManagedThreadId)
				workThread.Join();

			logger.Log(LogLevel.Debug, AgentDisconnectedID, "Remote protocol agent ({Name}): agent disconnected", name);
		}

		public void Reconnect(TcpClient newClient)
		{
			if (client.IsConnected)
				client.Close();

			client = new(newClient, logger, "RPA: " + name);

			if (workThread.IsAlive && workThread.ManagedThreadId != Environment.CurrentManagedThreadId)
				workThread.Join();

			workThread = new Thread(WorkThreadHandler);


			var newWorkThreadDispatcher = new ThreadDispatcher<WriteThreadTask>(workThread, WriteMessage);

			if (workThreadDispatcher.IsWorking)
			{
				foreach (var item in workThreadDispatcher.GetQueue())
					newWorkThreadDispatcher.DispatchTask(item);

				workThreadDispatcher.Close();
			}

			workThreadDispatcher = newWorkThreadDispatcher;


			logger.Log(LogLevel.Debug, AgentReconnectedID, "Remote protocol agent ({Name}): agent reconnected to new endpoint {Endpoint}", name, client.EndPoint);
		}

		public Task SendMessageAsync(ProtocolMessage message)
		{
			var tcs = new TaskCompletionSource();
			workThreadDispatcher.DispatchTask(new(message, tcs));

			logger.Log(LogLevel.Trace, NewMessageDispatchedID, "Remote protocol agent ({Name}): New message dispatched - {message}", name, message);

			return tcs.Task;
		}

		private void WorkThreadHandler()
		{
			while (true)
			{
				try
				{
					var index = workThreadDispatcher.WaitHandles(ReadOnlySpan<WaitHandle>.Empty, 100);

					if (index == ThreadDispatcherStatic.ClosedIndex)
					{
						return;
					}
					else if (index == ThreadDispatcherStatic.TimeoutIndex)
					{
						if (ExecutePeriodicTasks() == false) return;
					}
					else //ThreadDispatcherStatic.NewTaskIndex
					{
						LinkedList<Exception>? exceptions = null;
						var flag = true;

						while (flag)
						{
							try
							{
								flag = workThreadDispatcher.ExecuteTask();
							}
							catch (Exception ex)
							{
								exceptions ??= new();
								exceptions.AddLast(ex);
							}
						}

						if (exceptions is not null)
							throw new AggregateException(exceptions.ToArray());
					}
				}
				catch (Exception ex)
				{
					agentWorker.HandleError(this, ex);
					agentWorker.ScheduleReconnect(this);
					return;
				}
			}
		}

		private bool ExecutePeriodicTasks()
		{
			if (client.IsConnected == false)
			{
				logger.Log(LogLevel.Error, AgentConnectionLostID, "Remote protocol agent ({Name}): agent connection lost", name);
				agentWorker.ScheduleReconnect(this);
				return false;
			}


			while (client.IsDataAvailable)
			{
				try
				{
					var messageContent = client.ReadObject<MessageSerializationModel>();

					List<ProtocolMessage.Attachment>? attachments = null;

					if (messageContent.Attachments is not null)
					{
						attachments = new();

						foreach (var attachment in messageContent.Attachments)
						{
							var buffer = client.ReadBytes(attachment.Value);
							attachments.Add(new(attachment.Key, new BufferCopier(buffer).CopyToAsync, attachment.Value));
						}
					}

					var body = SerializationContext.Instance.SubDeserialize(messageContent.Body, bodyTypeResolver.Resolve(new(messageContent.BodyType!, messageContent.Domain)));

					var message = new ProtocolMessage(new DateTime(messageContent.TimeSpamp), messageContent.Domain, body, attachments, messageContent.DebugMessage);


					logger.Log(LogLevel.Trace, NewMessageHandledID, "Remote protocol agent ({Name}): new message handled - {message}", name, message);
					agentWorker.DispatchMessage(this, message);
				}
				catch (Exception ex)
				{
					logger.Log(LogLevel.Error, MessageHandleErrorID, ex, "Remote protocol agent ({Name}): message handle error", name);
				}
			}

			return true;
		}

		private void WriteMessage(WriteThreadTask task)
		{
			try
			{
				var message = task.Message;

				Dictionary<string, int>? attachmentLengths = null;
				if (message.Attachments is not null)
				{
					attachmentLengths = new();

					foreach (var attachment in message.Attachments.Values)
						attachmentLengths.Add(attachment.Name, attachment.Length);
				}

				string? bodyTypePseudoName = null;

				if (message.Body is not null)
				{
					var fullBodyTypeName = bodyTypeResolver.Code(message.Body.GetType());
					if (message.Domain != fullBodyTypeName.TargetDomain)
						throw new ArgumentException("Domain for body and marked in message are not equals");
					bodyTypePseudoName = fullBodyTypeName.PseudoTypeName;
				}

				var messageModel = new MessageSerializationModel(
					message.TimeStamp.Ticks,
					message.Domain,
					bodyTypePseudoName,
					message.Body,
					message.DebugMessage,
					attachmentLengths
				);

				var data = SerializationContext.Instance.Serialize(messageModel);

				try
				{
					client.WriteObject(data);

					if (message.Attachments is not null)
						foreach (var attachment in message.Attachments.Values)
						{
							client.CopyFrom(attachment.CopyDelegate);
						}
				}
				catch (Exception ex)
				{
					logger.Log(LogLevel.Warning, MessageSendErrorID, ex, "Remote protocol agent ({Name}): enable to send message, message will be resent", name);
					workThreadDispatcher.DispatchTask(task);
					return;
				}

				logger.Log(LogLevel.Trace, NewMessageWritenID, "Remote protocol agent ({Name}): new message written - {message}", name, message);
				task.Task.SetResult();
			}
			catch (Exception ex)
			{
				task.Task.SetException(ex);
				throw;
			}
		}


		private record WriteThreadTask(ProtocolMessage Message, TaskCompletionSource Task);

		private class BufferCopier
		{
			private readonly ReadOnlyMemory<byte> bufferToContain;


			public BufferCopier(ReadOnlyMemory<byte> bufferToContain)
			{
				this.bufferToContain = bufferToContain;
			}


			public ValueTask CopyToAsync(Stream target)
			{
				return target.WriteAsync(bufferToContain);
			}
		}

		private record MessageSerializationModel(
			long TimeSpamp,
			string Domain,
			string? BodyType,
			object? Body,
			string? DebugMessage,
			IReadOnlyDictionary<string, int>? Attachments
		);
	}
}
