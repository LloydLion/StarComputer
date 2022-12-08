using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using StarComputer.Shared.Connection;
using StarComputer.Shared.Protocol;
using StarComputer.Shared.Utils;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;

namespace StarComputer.Client
{
	internal class Client : IClient
	{
		private readonly ClientConfiguration options;
		private readonly AutoResetEvent onDisconnect = new(false);
		private readonly IMessageHandler messageHandler;


		public Client(IOptions<ClientConfiguration> options, IMessageHandler messageHandler)
		{
			this.options = options.Value;
			this.messageHandler = messageHandler;
		}


		public void Connect(IPEndPoint endPoint, string serverPassword, string login)
		{
			var rawClient = new TcpClient();
			rawClient.Connect(endPoint);

			var client = new SocketClient(rawClient);

			client.WriteJson(new ConnectionRequest(login, serverPassword, options.TargetProtocolVersion));
			while (client.IsDataAvailable == false) Thread.Sleep(10);
			var responce = client.ReadJson<ConnectionResponce>();
			
			if (responce.DebugMessage is not null)
				Console.WriteLine("Debug: " + responce.DebugMessage);

			if (responce.StatusCode != ConnectionStausCode.Successful)
				throw new Exception($"Connection failed: {responce.StatusCode}");

			var bodyJson = responce.ResponceBody ?? throw new NullReferenceException();
			var body = bodyJson.ToObject<SuccessfulConnectionResultBody>() ?? throw new NullReferenceException();

			var port = body.ConnectionPort;

			client.Close();

			//-----------------
			
			rawClient = new TcpClient();
			rawClient.Connect(new IPEndPoint(endPoint.Address, port));

			var onSomething = new AutoResetEvent(false);
			var agent = new Agent(onSomething);

			var remote = new RemoteProtocolAgent(rawClient, agent);

			remote.Start();

			try
			{
				remote.SendMessageAsync(new ProtocolMessage("MAIN", "+", null, "Hello!")).Wait();
			}
			catch (Exception ex) { Console.WriteLine(ex); }

		waitNew:
			onSomething.WaitOne();

			if (agent.Reason == Agent.SomethingReason.Reconnect)
			{
				rawClient.Close();
				rawClient = new TcpClient();
				rawClient.Connect(new IPEndPoint(endPoint.Address, port));

				remote.Reconnect(rawClient);

				goto waitNew;
			}
			else if (agent.Reason == Agent.SomethingReason.Disconnect)
			{
				Console.WriteLine("Done");
			}
			else
			{
				while (agent.Messages.TryDequeue(out var el))
				{
					messageHandler.HandleMessageAsync(el.Item2, el.Item1);
				}

				goto waitNew;
			}
		}


		private class Agent : IRemoteAgentWorker
		{
			private readonly AutoResetEvent onSomething;


			public Agent(AutoResetEvent onSomething)
			{
				this.onSomething = onSomething;
			}


			public void HandleDisconnect(RemoteProtocolAgent agent)
			{
				Reason = SomethingReason.Disconnect;
				onSomething.Set();
			}

			public void HandleError(RemoteProtocolAgent agent, Exception ex)
			{
				Console.WriteLine(ex.ToString());
			}

			public void ScheduleReconnect(RemoteProtocolAgent agent)
			{
				Reason = SomethingReason.Reconnect;
				onSomething.Set();
			}

			public void DispatchMessage(RemoteProtocolAgent agent, ProtocolMessage message)
			{
				Reason = SomethingReason.Message;
				Messages.Enqueue((agent, message));
				onSomething.Set();
			}


			public SomethingReason Reason { get; private set; }

			public ConcurrentQueue<(RemoteProtocolAgent, ProtocolMessage)> Messages { get; } = new();


			public enum SomethingReason
			{
				Disconnect,
				Reconnect,
				Message
			}
		}
	}
}
