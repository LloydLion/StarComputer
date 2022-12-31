using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StarComputer.Common.Protocol;
using StarComputer.Common.Abstractions.Utils;
using System.Net;
using System.Net.Sockets;
using StarComputer.Client.Abstractions;
using StarComputer.Common.Abstractions.Protocol;
using StarComputer.Common.Abstractions.Connection;
using StarComputer.Common.Abstractions.Threading;
using StarComputer.Common.Abstractions.Plugins;
using StarComputer.Common.Abstractions.Protocol.Bodies;

namespace StarComputer.Client
{
	public class Client : IClient
	{
		private static readonly EventId ClientReadyID = new(10, "ClientReady");
		private static readonly EventId WaitingNewTasksID = new(11, "WaitingNewTasks");
		private static readonly EventId CloseSignalRecivedID = new(12, "CloseSignalRecived");
		private static readonly EventId NewConnectionToServerID = new(13, "NewConnectionToServer");
		private static readonly EventId ClientDataSentID = new(14, "ClientDataSent");
		private static readonly EventId ConnectionAsClientID = new(15, "ConnectionAsClient");
		private static readonly EventId ClientJoinedID = new(16, "ClientJoined");
		private static readonly EventId ClientRejoinedID = new(17, "ClientRejoined");
		private static readonly EventId ConnectionLostID = new(18, "ClientConnectionLost");
		private static readonly EventId DisconnectedID = new(19, "Disconnected");
		private static readonly EventId MessageRecivedID = new(31, "MessageRecived");
		private static readonly EventId ExecutingNewTaskID = new(32, "ExecutingNewTask");
		private static readonly EventId FailedToExecuteTaskID = new(33, "FailedToExecuteTask");
		private static readonly EventId DebugMessageFromServerID = new(34, "DebugMessageFromServer");
		private static readonly EventId ClientConnectionFailID = new(21, "ClientConnectionFail");
		private static readonly EventId ClientJoinFailID = new(22, "ClientJoinFail");
		private static readonly EventId ClientRejoinFailID = new(23, "ClientRejoinFail");
		private static readonly EventId ProtocolErrorID = new(24, "ProtocolError");


		private readonly ClientConfiguration options;
		private readonly IMessageHandler messageHandler;
		private readonly ILogger<Client> logger;
		private readonly IThreadDispatcher<Action> mainThreadDispatcher;
		private readonly IBodyTypeResolver bodyTypeResolver;
		private readonly ManualResetEvent onConnectionEvent = new(false);
		private IRemoteProtocolAgent? serverAgent = null;
		private ConnectionConfiguration? connectionConfiguration = null;


		public Client(IOptions<ClientConfiguration> options,
				IMessageHandler messageHandler,
				ILogger<Client> logger,
				IThreadDispatcher<Action> mainThreadDispatcher,
				IBodyTypeResolver bodyTypeResolver)
		{
			this.options = options.Value;
			this.messageHandler = messageHandler;
			this.logger = logger;
			this.mainThreadDispatcher = mainThreadDispatcher;
			this.bodyTypeResolver = bodyTypeResolver;
		}


		public void Connect(ConnectionConfiguration connectionConfiguration)
		{
			this.connectionConfiguration = connectionConfiguration;
			onConnectionEvent.Set();
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

		public void MainLoop(IPluginStore plugins)
		{
			logger.Log(LogLevel.Information, ClientReadyID, "Client ready to connection to a server");

			onConnectionEvent.WaitOne();

			(IPEndPoint endPoint, string serverPassword, string login) = connectionConfiguration!.Value;

			var rawClient = new TcpClient();
			rawClient.Connect(endPoint);

			logger.Log(LogLevel.Information, NewConnectionToServerID, "New connection to {ServerEndPoint}", endPoint);

			int port;

			try
			{
				var client = new SocketClient(rawClient, logger);

				var pluginsVersions = plugins.ToDictionary(s => s.Key, s => s.Value.Version);
				client.WriteJson(new ConnectionRequest(login, serverPassword, options.TargetProtocolVersion, pluginsVersions));

				logger.Log(LogLevel.Debug, ClientDataSentID, "Client data sent to server at {ServerEndPoint}. Login - {Login}, password - {Password}", endPoint, login, serverPassword);

				while (client.IsDataAvailable == false) Thread.Sleep(10);
				var responce = client.ReadJson<ConnectionResponce>();

				if (responce.DebugMessage is not null)
					logger.Log(LogLevel.Debug, DebugMessageFromServerID, "Debug message from server: {DebugMessage}", responce.DebugMessage);

				if (responce.StatusCode != ConnectionStausCode.Successful)
					throw new Exception($"Connection failed: {responce.StatusCode}");

				var bodyJson = responce.ResponceBody ?? throw new NullReferenceException();
				var body = bodyJson.ToObject<SuccessfulConnectionResultBody>() ?? throw new NullReferenceException();

				port = body.ConnectionPort;

				client.Close();

				logger.Log(LogLevel.Information, ConnectionAsClientID, "Connected to server, join port - {JoinPort}", port);
			}
			catch (Exception ex)
			{
				logger.Log(LogLevel.Error, ClientConnectionFailID, ex, "Failed to connect to the server");
				return;
			}

			try
			{
				var endpoint = new IPEndPoint(endPoint.Address, port);

				rawClient = new TcpClient();
				rawClient.Connect(endpoint);

				var agent = new AgentWorker(this, endpoint);

				serverAgent = new RemoteProtocolAgent(rawClient, agent, logger, bodyTypeResolver);

				serverAgent.Start();

				logger.Log(LogLevel.Information, ClientJoinedID, "Joined to {ServerEndPoint} as ({Login}[{LocalIP}])",
					endPoint, login, (IPEndPoint)rawClient.Client.LocalEndPoint!);
			}
			catch (Exception ex)
			{
				logger.Log(LogLevel.Error, ClientJoinFailID, ex, "Failed to join to server at {ServerEndPoint}", endPoint);
				return;
			}


			while (true)
			{
				logger.Log(LogLevel.Trace, WaitingNewTasksID, "Waiting for new task or server message");
				var index = mainThreadDispatcher.WaitHandles(ReadOnlySpan<WaitHandle>.Empty);


				if (index == -2)
				{
					var flag = true;
					while (flag)
					{
						try
						{
							logger.Log(LogLevel.Trace, ExecutingNewTaskID, "Executing new client task");
							flag = mainThreadDispatcher.ExecuteTask();
						}
						catch (Exception ex)
						{
							logger.Log(LogLevel.Error, FailedToExecuteTaskID, ex, "Failed to execute some task");
						}
					}
				}
				else //-1
				{
					logger.Log(LogLevel.Information, CloseSignalRecivedID, "Client closing by internal command");
					return;
				}
			}
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
				owner.logger.Log(LogLevel.Information, DisconnectedID, "Disconnection from server, session end, client stop work");
				owner.mainThreadDispatcher.Close();
			}

			public void HandleError(IRemoteProtocolAgent agent, Exception ex)
			{
				owner.logger.Log(LogLevel.Error, ProtocolErrorID, ex, "Error in protocol agent for server");
			}

			public void ScheduleReconnect(IRemoteProtocolAgent agent)
			{
				owner.logger.Log(LogLevel.Information, ConnectionLostID, "Connection to server lost");
				owner.mainThreadDispatcher.DispatchTask(async () =>
				{
					await Task.Delay(owner.options.ServerReconnectionPrepareTimeout);

					var client = new TcpClient();

					try { client.Connect(connectionPoint); }
					catch (SocketException)
					{
						owner.logger.Log(LogLevel.Error, ClientRejoinFailID, "Failed to rejoin to server, connection terminated");
						agent.Disconnect();
						return;
					}

					agent.Reconnect(client);
					owner.logger.Log(LogLevel.Error, ClientRejoinedID, "Client rejoined to server");
				});
			}

			public void DispatchMessage(IRemoteProtocolAgent agent, ProtocolMessage message)
			{
				owner.logger.Log(LogLevel.Debug, MessageRecivedID, "New message recived from server\n\t{Message}", message);

				owner.mainThreadDispatcher.DispatchTask(() =>
				{
					owner.messageHandler.HandleMessageAsync(message, agent);
				});
			}
		}
	}
}
