using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Concurrent;
using System.Net.Sockets;
using System.Text;

namespace StarComputer.Shared.Protocol
{
	public class RemoteProtocolAgent
	{
		private const char RequestPrefix = '#';
		private const char ResponcePrefix = '$';


		private TcpClient client;
		private Stream targetStream;
		private StreamReader reader;
		private StreamWriter writer;

		private readonly IMessageHandler messageHandler;
		private readonly IRemoteAgentWorker agentWorker;

		private Thread readThread;
		private readonly ConcurrentQueue<RemoteResponce> responces = new();
		private readonly AutoResetEvent onNewResponce = new(false);

		private Thread writeThread;
		private readonly ConcurrentQueue<WriteThreadTask> tasks = new();
		private readonly AutoResetEvent onTaskEvent = new(false);
		private bool isClosing = false;


		public RemoteProtocolAgent(TcpClient client, IRemoteAgentWorker agentWorker, IMessageHandler messageHandler)
		{
			targetStream = client.GetStream();
			reader = new StreamReader(targetStream);
			writer = new StreamWriter(targetStream) { AutoFlush = true };

			readThread = new Thread(ReadThreadHandler);
			writeThread = new Thread(WriteThreadHandler);

			this.client = client;
			this.agentWorker = agentWorker;
			this.messageHandler = messageHandler;
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
			Disconnect();

			targetStream = newClient.GetStream();
			reader = new StreamReader(targetStream);
			writer = new StreamWriter(targetStream) { AutoFlush = true };

			readThread = new Thread(ReadThreadHandler);
			writeThread = new Thread(WriteThreadHandler);

			Start();
		}

		public Task<SendStatusCode> SendMessageAsync(ProtocolMessage message)
		{
			var tcs = new TaskCompletionSource<SendStatusCode>();
			tasks.Enqueue(new(message, tcs));
			onTaskEvent.Set();
			return tcs.Task;
		}

		private void DisconnectInternal()
		{
			isClosing = true;

			onTaskEvent.Set();
			onNewResponce.Set();

			writeThread.Join();
			readThread.Join();

			client.Close();
		}

		private void ReadThreadHandler()
		{
			var cell = new char[1];

			while (isClosing == false)
			{
				try
				{
					Thread.Sleep(100);
					if (isClosing) return;

					while (client.Available != 0)
					{
						reader.Read(cell.AsSpan());
						var sym = cell[1];

						var line = reader.ReadLine() ?? throw new NullReferenceException();

						if (sym == ResponcePrefix)
						{
							var responce = JsonConvert.DeserializeObject<RemoteResponce>(line);
							responces.Enqueue(responce);
							onNewResponce.Set();
						}
						else
						{
							var messageContent = JsonConvert.DeserializeObject<MessageJsonContent>(line) ?? throw new NullReferenceException();

							List<ProtocolMessage.Attachment>? attachments = null;

							if (messageContent.AttachmentLengths is not null)
							{
								attachments = new();

								foreach (var attachment in messageContent.AttachmentLengths)
								{
									var buffer = new byte[attachment.Value];
									targetStream.Read(buffer.AsSpan());
									attachments.Add(new(attachment.Key, new BufferCopier(buffer).CopyToAsync, attachment.Value));
								}
							}

							var body = messageContent.Body?.ToObject(Type.GetType(messageContent.BodyTypeAssemblyQualifiedName ?? throw new NullReferenceException(), true) ?? throw new NullReferenceException());

							var message = new ProtocolMessage(new DateTime(messageContent.TimeStampUtcTicks), messageContent.ProviderDomain, body, attachments, messageContent.DebugMessage);


							var code = messageHandler.HandleMessageAsync(message, this).Result;


							var responce = new RemoteResponce(code);
							writer.Write(ResponcePrefix);
							writer.WriteLine(JsonConvert.SerializeObject(responce));
						}
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
				onTaskEvent.WaitOne();
				if (isClosing) return;

				try
				{
					while (tasks.IsEmpty == false)
						if (tasks.TryDequeue(out var task))
						{
							var message = task.Message;

							Dictionary<string, long>? attachmentLengths = null;
							if (message.Attachments is not null)
							{
								attachmentLengths = new();

								foreach (var attachment in message.Attachments.Values)
									attachmentLengths.Add(attachment.Name, attachment.Length);
							}


							var json = JsonConvert.SerializeObject(new MessageJsonContent(
								message.TimeStamp.Ticks,
								message.Domain,
								message.Body?.GetType().AssemblyQualifiedName,
								message.Body is null ? null : JToken.FromObject(message.Body),
								message.DebugMessage,
								attachmentLengths
							));


							writer.Write(RequestPrefix);
							writer.WriteLine(json);

							if (message.Attachments is not null)
							{
								foreach (var attachment in message.Attachments.Values)
									attachment.CopyDelegate.Invoke(targetStream).AsTask().Wait();
							}


							onNewResponce.WaitOne();
							if (isClosing) return;

							if (responces.TryDequeue(out var responce))
								task.Task.SetResult(responce.StatusCode);
							else task.Task.SetException(new Exception("Something went wrong!"));
						}
				}
				catch (Exception ex)
				{
					agentWorker.HandleError(this, ex);
					agentWorker.ScheduleReconnect(this);
				}
			}
		}


		private record struct WriteThreadTask(ProtocolMessage Message, TaskCompletionSource<SendStatusCode> Task);

		private record struct RemoteResponce(SendStatusCode StatusCode);

		private class BufferCopier
		{
			private readonly byte[] bufferToContain;


			public BufferCopier(byte[] bufferToContain)
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
			[JsonProperty(PropertyName = "AttachmentTable")] IReadOnlyDictionary<string, long>? AttachmentLengths
		);
	}
}
