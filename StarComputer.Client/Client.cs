﻿using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StarComputer.Common.Protocol;
using StarComputer.Common.Abstractions.Utils;
using System.Net;
using System.Net.Sockets;
using StarComputer.Client.Abstractions;
using StarComputer.Common.Abstractions.Protocol;
using StarComputer.Common.Abstractions.Connection;
using StarComputer.Common.Abstractions.Threading;

namespace StarComputer.Client
{
	public class Client : IClient
	{
		private readonly ClientConfiguration options;
		private readonly IMessageHandler messageHandler;
		private readonly ILogger<Client> logger;
		private readonly IThreadDispatcher<Action> mainThreadDispatcher;
		private IRemoteProtocolAgent? serverAgent = null;
		private ConnectionConfiguration? connectionConfiguration = null;


		public Client(IOptions<ClientConfiguration> options, IMessageHandler messageHandler, ILogger<Client> logger, IThreadDispatcher<Action> mainThreadDispatcher)
		{
			this.options = options.Value;
			this.messageHandler = messageHandler;
			this.logger = logger;
			this.mainThreadDispatcher = mainThreadDispatcher;
		}


		public void Connect(ConnectionConfiguration connectionConfiguration)
		{
			(IPEndPoint endPoint, string serverPassword, string login) = connectionConfiguration;
			this.connectionConfiguration = connectionConfiguration;

			var rawClient = new TcpClient();
			rawClient.Connect(endPoint);

			var client = new SocketClient(rawClient, logger);

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

			var agent = new AgentWorker(this, endpoint);

			serverAgent = new RemoteProtocolAgent(rawClient, agent, logger);

			serverAgent.Start();

			SynchronizationContext.SetSynchronizationContext(mainThreadDispatcher.CraeteSynchronizationContext(s => s));


			while (true)
			{
				var index = mainThreadDispatcher.WaitHandles(ReadOnlySpan<WaitHandle>.Empty);


				if (index == -2)
				{
					var flag = true;
					while (flag)
					{
						try
						{
							flag = mainThreadDispatcher.ExecuteTask();
						}
						catch (Exception)
						{

						}
					}
				}
				else //-1
				{
					
					return;
				}
			}
		}

		public ConnectionConfiguration GetConnectionConfiguration()
		{
			if (connectionConfiguration is null)
				throw new InvalidOperationException("Connect to server before get agent");
			return connectionConfiguration.Value;
		}

		public IRemoteProtocolAgent GetServerAgent()
		{
			if (serverAgent is null)
				throw new InvalidOperationException("Connect to server before get agent");
			return serverAgent;
		}


		private class AgentWorker : IRemoteAgentWorker
		{
			private readonly Client owner;
			private readonly IPEndPoint connectionPoint;


			public AgentWorker(Client owner, IPEndPoint connectionPoint)
			{
				this.owner = owner;
				this.connectionPoint = connectionPoint;
			}


			public void HandleDisconnect(IRemoteProtocolAgent agent)
			{
				owner.mainThreadDispatcher.Close();
			}

			public void HandleError(IRemoteProtocolAgent agent, Exception ex)
			{
				Console.WriteLine(ex.ToString());
			}

			public void ScheduleReconnect(IRemoteProtocolAgent agent)
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

			public void DispatchMessage(IRemoteProtocolAgent agent, ProtocolMessage message)
			{
				owner.mainThreadDispatcher.DispatchTask(() =>
				{
					owner.messageHandler.HandleMessageAsync(message, agent);
				});
			}
		}
	}
}
