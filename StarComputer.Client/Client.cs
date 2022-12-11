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
		private readonly IMessageHandler messageHandler;
		private readonly ThreadDispatcher<Action> mainThreadDispatcher;


		public Client(IOptions<ClientConfiguration> options, IMessageHandler messageHandler)
		{
			this.options = options.Value;
			this.messageHandler = messageHandler;

			mainThreadDispatcher = new(Thread.CurrentThread, s => s());
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

			var endpoint = new IPEndPoint(endPoint.Address, port);

			rawClient = new TcpClient();
			rawClient.Connect(endpoint);

			var agent = new Agent(this, endpoint);

			var remote = new RemoteProtocolAgent(rawClient, agent);

			remote.Start();

			SynchronizationContext.SetSynchronizationContext(mainThreadDispatcher.CraeteSynchronizationContext(s => s));


			mainThreadDispatcher.DispatchTask(() =>
				remote.SendMessageAsync(new ProtocolMessage("MAIN", "+", null, "Hello!")));


			while (true)
			{
				var index = mainThreadDispatcher.WaitHandlers();


				if (index == -2)
				{
					try
					{
						mainThreadDispatcher.ExecuteAllTasks();
					}
					catch (Exception ex)
					{
						Console.WriteLine("!!!" + ex.ToString());
					}
				}
				else //-1
				{
					
					return;
				}
			}
		}


		private class Agent : IRemoteAgentWorker
		{
			private readonly Client owner;
			private readonly IPEndPoint connectionPoint;


			public Agent(Client owner, IPEndPoint connectionPoint)
			{
				this.owner = owner;
				this.connectionPoint = connectionPoint;
			}


			public void HandleDisconnect(RemoteProtocolAgent agent)
			{
				owner.mainThreadDispatcher.Close();
			}

			public void HandleError(RemoteProtocolAgent agent, Exception ex)
			{
				Console.WriteLine(ex.ToString());
			}

			public void ScheduleReconnect(RemoteProtocolAgent agent)
			{
				owner.mainThreadDispatcher.DispatchTask(async () =>
				{
					await Task.Delay(owner.options.ServerReconnectionPrepareTimeout);

					var client = new TcpClient();

					try { client.Connect(connectionPoint); }
					catch (SocketException)
					{
						agent.Disconnect();
						return;
					}

					agent.Reconnect(client);
				});
			}

			public void DispatchMessage(RemoteProtocolAgent agent, ProtocolMessage message)
			{
				owner.mainThreadDispatcher.DispatchTask(() =>
				{
					owner.messageHandler.HandleMessageAsync(message, agent);
				});
			}
		}
	}
}
