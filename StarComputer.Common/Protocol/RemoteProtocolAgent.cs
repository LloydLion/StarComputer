using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
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
		private SocketClient client;
		private readonly IRemoteAgentWorker agentWorker;
		private readonly ILogger logger;
		private readonly IBodyTypeResolver bodyTypeResolver;

		private Thread workThread;
		private IThreadDispatcher<WriteThreadTask> workThreadDispatcher;


		public IPEndPoint CurrentEndPoint => client.EndPoint;


		public RemoteProtocolAgent(TcpClient client, IRemoteAgentWorker agentWorker, ILogger logger, IBodyTypeResolver bodyTypeResolver)
		{
			this.client = new(client, logger);

			workThread = new Thread(WorkThreadHandler);
			workThreadDispatcher = new ThreadDispatcher<WriteThreadTask>(workThread, WriteMessage);

			this.agentWorker = agentWorker;
			this.logger = logger;
			this.bodyTypeResolver = bodyTypeResolver;
		}


		public void Start()
		{
			workThread.Start();
		}

		public void Disconnect()
		{
			DisconnectInternal();
			agentWorker.HandleDisconnect(this);
		}

		public void Reconnect(TcpClient newClient)
		{
			client = new(newClient, logger);

			workThread = new Thread(WorkThreadHandler);
			workThreadDispatcher = new ThreadDispatcher<WriteThreadTask>(workThread, WriteMessage);

			Start();
		}

		public Task SendMessageAsync(ProtocolMessage message)
		{
			var tcs = new TaskCompletionSource();
			workThreadDispatcher.DispatchTask(new(message, tcs));
			return tcs.Task;
		}

		private void DisconnectInternal()
		{
			workThreadDispatcher.Close();

			if (Environment.CurrentManagedThreadId != workThread.ManagedThreadId)
				workThread.Join();

			client.Close();
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
						var queue = workThreadDispatcher.GetQueue();
						foreach (var task in queue)
							task.Task.SetException(new Exception("Protocol agent closed"));
						return;
					}
					else if (index == ThreadDispatcherStatic.TimeoutIndex)
					{
						ExecutePeriodicTasks();
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
								if (exceptions is null)
									exceptions = new();
								exceptions.AddLast(ex);
							}
						}

						if (exceptions is not null)
							throw new AggregateException(exceptions.ToArray());
					}
				}
				catch (Exception ex)
				{
					DisconnectInternal();
					agentWorker.HandleError(this, ex);
					agentWorker.ScheduleReconnect(this);
					return;
				}
			}
		}

		private void ExecutePeriodicTasks()
		{
			if (client.IsConnected == false)
			{
				DisconnectInternal();
				agentWorker.ScheduleReconnect(this);
				return;
			}

			while (client.IsDataAvailable)
			{
				var messageContent = client.ReadJson<MessageJsonContent>();

				List<ProtocolMessage.Attachment>? attachments = null;

				if (messageContent.AttachmentLengths is not null)
				{
					attachments = new();

					foreach (var attachment in messageContent.AttachmentLengths)
					{
						var buffer = client.ReadBytes(attachment.Value);
						attachments.Add(new(attachment.Key, new BufferCopier(buffer).CopyToAsync, attachment.Value));
					}
				}

				var body = messageContent.Body?.ToObject(bodyTypeResolver.Resolve(new(messageContent.BodyType!, messageContent.MessageDomain)));

				var message = new ProtocolMessage(new DateTime(messageContent.TimeStampUtcTicks), messageContent.MessageDomain, body, attachments, messageContent.DebugMessage);


				agentWorker.DispatchMessage(this, message);
			}
		}

		private void WriteMessage(WriteThreadTask task)
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

			var jsonObject = new MessageJsonContent(
				message.TimeStamp.Ticks,
				message.Domain,
				bodyTypePseudoName,
				message.Body is null ? null : JToken.FromObject(message.Body),
				message.DebugMessage,
				attachmentLengths
			);


			client.WriteJson(jsonObject);

			if (message.Attachments is not null)
				foreach (var attachment in message.Attachments.Values)
					client.CopyFrom(attachment.CopyDelegate);

			task.Task.SetResult();
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

		private record MessageJsonContent(
			[JsonProperty(PropertyName = "TimeStamp")] long TimeStampUtcTicks,
			[JsonProperty(PropertyName = "Domain")] string MessageDomain,
			[JsonProperty(PropertyName = "BodyType")] string? BodyType,
			[JsonProperty(PropertyName = "Body")] JToken? Body,
			[JsonProperty(PropertyName = "Debug")] string? DebugMessage,
			[JsonProperty(PropertyName = "AttachmentTable")] IReadOnlyDictionary<string, int>? AttachmentLengths
		);
	}
}
