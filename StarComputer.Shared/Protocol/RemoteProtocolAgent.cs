using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using StarComputer.Shared.Utils;
using System.Net;
using System.Net.Sockets;

namespace StarComputer.Shared.Protocol
{
	public class RemoteProtocolAgent
	{
		private SocketClient client;
		private readonly IRemoteAgentWorker agentWorker;
		private readonly ILogger logger;

		private Thread readThread;
		private Thread writeThread;
		private ThreadDispatcher<WriteThreadTask> writeThreadDispatcher;

		private bool isClosing = false;


		public IPEndPoint CurrentEndPoint => client.EndPoint;


		public RemoteProtocolAgent(TcpClient client, IRemoteAgentWorker agentWorker, ILogger logger)
		{
			this.client = new(client, logger);

			readThread = new Thread(ReadThreadHandler);
			writeThread = new Thread(WriteThreadHandler);
			writeThreadDispatcher = new(writeThread, WriteMessage);

			this.agentWorker = agentWorker;
			this.logger = logger;
		}


		public void Start()
		{
			readThread.Start();
			writeThread.Start();
		}

		public void Disconnect()
		{
			DisconnectInternal();
			agentWorker.HandleDisconnect(this);
		}

		public void Reconnect(TcpClient newClient)
		{
			DisconnectInternal();

			client = new(newClient, logger);

			readThread = new Thread(ReadThreadHandler);
			writeThread = new Thread(WriteThreadHandler);
			writeThreadDispatcher = new(writeThread, WriteMessage);

			isClosing = false;

			Start();
		}

		public Task SendMessageAsync(ProtocolMessage message)
		{
			var tcs = new TaskCompletionSource();
			writeThreadDispatcher.DispatchTask(new(message, tcs));
			return tcs.Task;
		}

		private void DisconnectInternal()
		{
			isClosing = true;

			writeThreadDispatcher.Close();

			writeThread.Join();
			readThread.Join();

			client.Close();
		}

		private void ReadThreadHandler()
		{
			while (isClosing == false)
			{
				try
				{
					Thread.Sleep(100);

					if (client.IsConnected == false)
					{
						agentWorker.ScheduleReconnect(this);
						return;
					}

					if (isClosing) return;

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

						var body = messageContent.Body?.ToObject(Type.GetType(messageContent.BodyTypeAssemblyQualifiedName ?? throw new NullReferenceException(), true) ?? throw new NullReferenceException());

						var message = new ProtocolMessage(new DateTime(messageContent.TimeStampUtcTicks), messageContent.ProviderDomain, body, attachments, messageContent.DebugMessage);


						agentWorker.DispatchMessage(this, message);
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

		private void WriteThreadHandler()
		{
			while (isClosing == false)
			{
				var index = writeThreadDispatcher.WaitHandlers();

				if (index == WaitHandle.WaitTimeout)
				{
					//.... periodic read task
				}

				if (index == -1)
				{
					var queue = writeThreadDispatcher.GetQueueUnsafe();
					while (queue.TryDequeue(out var task))
						task.Task.SetResult();
					return;
				}

				try
				{
					writeThreadDispatcher.ExecuteAllTasks();
				}
				catch (Exception ex)
				{
					agentWorker.HandleError(this, ex);
					agentWorker.ScheduleReconnect(this);
				}
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


			var jsonObject = new MessageJsonContent(
				message.TimeStamp.Ticks,
				message.Domain,
				message.Body?.GetType().AssemblyQualifiedName,
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
			[JsonProperty(PropertyName = "Domain")] string ProviderDomain,
			[JsonProperty(PropertyName = "BodyType")] string? BodyTypeAssemblyQualifiedName,
			[JsonProperty(PropertyName = "Body")] JToken? Body,
			[JsonProperty(PropertyName = "Debug")] string? DebugMessage,
			[JsonProperty(PropertyName = "AttachmentTable")] IReadOnlyDictionary<string, int>? AttachmentLengths
		);
	}
}
