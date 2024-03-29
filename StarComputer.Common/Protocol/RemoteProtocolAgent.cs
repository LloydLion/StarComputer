﻿using StarComputer.Common.Abstractions;
using StarComputer.Common.Abstractions.Protocol;
using StarComputer.Common.Abstractions.Protocol.Bodies;
using StarComputer.Common.Abstractions.Threading;
using System.Net;

namespace StarComputer.Common.Protocol
{
	public class RemoteProtocolAgent : IRemoteProtocolAgent
	{
		private readonly HttpClient client = new();
		private readonly CancellationTokenSource sendCts = new();
		private readonly Uri httpEndPoint;
		private readonly IBodyTypeResolver bodyTypeResolver;
		private readonly IThreadDispatcher<Action> mainThreadDispathcer;
		private readonly string messageSendRequestTypeHeader;
		private readonly Guid? uniqueClientId;


		public Guid UniqueAgentId { get; } = Guid.NewGuid();

		public bool IsAlive { get; private set; } = true;


		public RemoteProtocolAgent(Uri httpEndPoint, IBodyTypeResolver bodyTypeResolver, IThreadDispatcher<Action> mainThreadDispathcer, string messageSendRequestTypeHeader, Guid? uniqueClientId = null)
		{
			this.httpEndPoint = httpEndPoint;
			this.bodyTypeResolver = bodyTypeResolver;
			this.mainThreadDispathcer = mainThreadDispathcer;
			this.messageSendRequestTypeHeader = messageSendRequestTypeHeader;
			this.uniqueClientId = uniqueClientId;

			client.Timeout = StaticInformation.DefaultHttpTimeout;
		}


		public async Task SendMessageAsync(ProtocolMessage message)
		{
			var tcs = new TaskCompletionSource();

			if (IsAlive == false)
				throw new InvalidOperationException("Agent has been disconnected, enable to send message");

			var httpMessage = await HttpProtocolHelper.WriteMessageAsync(message, bodyTypeResolver);
			if (uniqueClientId is not null)
				HttpProtocolHelper.PasteClientUniqueID(httpMessage.Headers, uniqueClientId.Value);
			httpMessage.Headers.Add(HttpProtocolHelper.RequestTypeHeader, messageSendRequestTypeHeader);
			httpMessage.RequestUri = httpEndPoint;

			mainThreadDispathcer.DispatchTask(async () =>
			{
				try
				{
					var result = await client.SendAsync(httpMessage, sendCts.Token);

					if (result.IsSuccessStatusCode)
						tcs.SetResult();
					else
					{
						var content = await result.Content.ReadAsStringAsync();
						tcs.SetException(new Exception($"HTTP exception, opposite side returned status code [{result.StatusCode}]: {content}"));
					}
				}
				catch (Exception ex)
				{
					tcs.SetException(ex);
					throw;
				}
			});

			await tcs.Task;
		}

		public void Start()
		{
			
		}

		public void Disconnect()
		{
			client.Dispose();

			IsAlive = false;

			sendCts.Cancel();
		}


		private record MessageSendTask(TaskCompletionSource Task, ProtocolMessage Message);
	}
}
