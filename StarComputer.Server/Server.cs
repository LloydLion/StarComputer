﻿using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;
using StarComputer.Common.Abstractions;
using StarComputer.Common.Abstractions.Connection;
using StarComputer.Common.Abstractions.Protocol;
using StarComputer.Common.Protocol;
using StarComputer.Common.Abstractions.Utils;
using StarComputer.Server.Abstractions;
using System.Net.Sockets;
using StarComputer.Common.Abstractions.Threading;
using StarComputer.Common.Abstractions.Plugins;
using StarComputer.Common.Abstractions.Protocol.Bodies;

namespace StarComputer.Server
{
	public class Server : IServer
	{
		private static readonly EventId ServerReadyID = new(10, "ServerReady");
		private static readonly EventId WaitingNewTasksID = new(11, "WaitingNewTasks");
		private static readonly EventId CloseSignalRecivedID = new(12, "CloseSignalRecived");
		private static readonly EventId NewConnectionAcceptedID = new(13, "NewConnectionAccepted");
		private static readonly EventId NewClientAcceptedID = new(14, "NewClientAccepted");
		private static readonly EventId ClientConnectedID = new(15, "ClientConnected");
		private static readonly EventId ClientJoinedID = new(16, "ClientJoined");
		private static readonly EventId ClientRejoinedID = new(17, "ClientRejoined");
		private static readonly EventId ClientConnectionLostID = new(18, "ClientConnectionLost");
		private static readonly EventId ClientDisconnectedID = new(19, "ClientDisconnected");
		private static readonly EventId MessageRecivedID = new(31, "MessageRecived");
		private static readonly EventId ExecutingNewTaskID = new(32, "ExecutingNewTask");
		private static readonly EventId FailedToExecuteTaskID = new(33, "FailedToExecuteTask");
		private static readonly EventId ClientInitializeErrorID = new(21, "ClientInitializeError");
		private static readonly EventId ClientConnectionFailID = new(22, "ClientConnectionFail");
		private static readonly EventId ClientJoinFailID = new(23, "ClientJoinFail");
		private static readonly EventId ClientRejoinFailID = new(24, "ClientRejoinFail");
		private static readonly EventId ProtocolErrorID = new(25, "ProtocolError");


		private readonly TcpListener connectionListener;
		private readonly ServerConfiguration options;
		private readonly ILogger<Server> logger;
		private readonly IClientApprovalAgent clientApprovalAgent;
		private readonly PortRentManager portRent;
		private readonly IBodyTypeResolver bodyTypeResolver;

		private readonly Dictionary<Guid, ServerSideClientInternal> agents = new();
		private readonly IMessageHandler messageHandler;
		private readonly AgentWorker agentWorker;

		private readonly IThreadDispatcher<Action> mainThreadDispatcher;

		private readonly AutoResetEvent onServerClosed = new(false);

		private readonly AutoResetEvent onListeningEvent = new(false);
		private TaskCompletionSource? listeningTaskTCS = null;
		private bool isListening;


		public Server(
			IOptions<ServerConfiguration> options,
			ILogger<Server> logger,
			IClientApprovalAgent clientApprovalAgent,
			IMessageHandler messageHandler,
			IThreadDispatcher<Action> mainThreadDispatcher,
			IBodyTypeResolver bodyTypeResolver)
		{
			options.Value.Validate();

			this.options = options.Value;
			connectionListener = new TcpListener(options.Value.Interface, options.Value.ConnectionPort);
			this.logger = logger;
			this.clientApprovalAgent = clientApprovalAgent;
			this.messageHandler = messageHandler;
			portRent = new(options.Value.OperationsPortRange);
			agentWorker = new(this);

			this.mainThreadDispatcher = mainThreadDispatcher;
			this.bodyTypeResolver = bodyTypeResolver;
		}


		public bool IsListening { get => isListening; private set { isListening = value; ListeningStatusChanged?.Invoke(); } }


		public event Action? ListeningStatusChanged;


		public ValueTask ListenAsync()
		{
			listeningTaskTCS = new();
			onListeningEvent.Set();

			return new(listeningTaskTCS.Task);
		}

		public void MainLoop(IPluginStore plugins)
		{
		restart:
			onListeningEvent.WaitOne();
			try
			{
				connectionListener.Start(options.MaxPendingConnectionQueue);
				listeningTaskTCS?.SetResult();
				IsListening = true;
			}
			catch (Exception ex)
			{
				listeningTaskTCS?.SetException(ex);
				IsListening = false;
				goto restart;
			}

			var waitForClient = new AutoResetEvent(false);
			createClientTask(waitForClient, out var clientTask, out var clientTaskCancel);

			logger.Log(LogLevel.Information, ServerReadyID, "Server is ready and listen on {IP}:{Port}", options.Interface, options.ConnectionPort);

			Span<WaitHandle> alsoWait = new WaitHandle[2];
			alsoWait[0] = waitForClient;
			alsoWait[1] = onServerClosed;

			while (true)
			{
				logger.Log(LogLevel.Trace, WaitingNewTasksID, "Server is waiting new tasks or client connection");

				var index = mainThreadDispatcher.WaitHandles(alsoWait);

				if (index == ThreadDispatcherStatic.ClosedIndex)
				{
					throw new InvalidOperationException("Dispatcher is closed, by external command. Terminating server");
				}
				else if (index == 1)
				{
					logger.Log(LogLevel.Information, CloseSignalRecivedID, "Server has recived close signal, closing");

					IsListening = false;

					clientTaskCancel.Cancel();
					try { clientTask.Wait(); } catch (Exception) { }
					connectionListener.Stop();

					foreach (var agent in agents)
						agent.Value.ProtocolAgent.Disconnect();

					return;
				}
				else if (index == 0)
				{
					var rawClient = clientTask.Result;
					createClientTask(waitForClient, out clientTask, out clientTaskCancel);

					logger.Log(LogLevel.Information, NewConnectionAcceptedID, "New connection accepted from {IP}", rawClient.Client.RemoteEndPoint);

					try
					{
						var client = new SocketClient(rawClient, logger);

						try
						{
							var request = client.ReadObject<ConnectionRequest>();

							logger.Log(LogLevel.Information, NewClientAcceptedID, "New client accepted: {Login} ({IP}) v{Version}", request.Login, client.EndPoint, request.ProtocolVersion);

							var responce = ProcessClientConnection(request, new(request.Login, client.EndPoint), plugins);

							if (responce.StatusCode == ConnectionStausCode.Successful)
								logger.Log(LogLevel.Information, ClientConnectedID, "Client ({Login}[{IP}]) connected to server successfully", request.Login, client.EndPoint);
							else logger.Log(LogLevel.Error, ClientConnectionFailID, "Cannot connect client ({Login}[{IP}]). Status code {StatusCode}", request.Login, client.EndPoint, responce.StatusCode);

							client.WriteObject(responce);
						}
						catch (Exception ex)
						{
							client.WriteObject(new ConnectionResponce(ConnectionStausCode.ProtocolError, ex.ToString(), null, null));
							throw;
						}
					}
					catch (Exception ex)
					{
						logger.Log(LogLevel.Error, ClientInitializeErrorID, ex, "Failed to initialize client");
					}
				}
				else if (index == ThreadDispatcherStatic.NewTaskIndex)
				{
					var flag = true;
					while (flag)
					{
						try
						{
							logger.Log(LogLevel.Trace, ExecutingNewTaskID, "Server started to execute new task");
							flag = mainThreadDispatcher.ExecuteTask();
						}
						catch (Exception ex)
						{
							logger.Log(LogLevel.Error, FailedToExecuteTaskID, ex, "Failed to execute some task");
						}
					}
				}
			}


			void createClientTask(AutoResetEvent waitForClient, out Task<TcpClient> task, out CancellationTokenSource cancellationToken)
			{
				var cts = new CancellationTokenSource();
				task = connectionListener.AcceptTcpClientAsync(cts.Token).AsTask().ContinueWith(s => { waitForClient.Set(); return s.Result; });
				cancellationToken = cts;
			}
		}

		private ConnectionResponce ProcessClientConnection(ConnectionRequest request, ClientConnectionInformation clientInformation, IPluginStore plugins)
		{
			if (request.ProtocolVersion != options.TargetProtocolVersion)
				return new ConnectionResponce(ConnectionStausCode.IncompatibleVersion, $"Target version is {options.TargetProtocolVersion}", null, null);

			if (request.ServerPassword != options.ServerPassword)
				return new ConnectionResponce(ConnectionStausCode.InvalidPassword, request.ServerPassword, null, null);

			var versions = plugins.ToDictionary(s => s.Key, s => s.Value.Version);
			if (request.Plugins.SequenceEqual(versions) == false)
				return new ConnectionResponce(ConnectionStausCode.IncompatiblePluginVersion, "Invalid version of some plugin", null, null);

			if (portRent.TryRentPort(out var port))
			{
				ClientApprovalResult? approvalResult = null;

				try
				{
					approvalResult = clientApprovalAgent.ApproveClientAsync(clientInformation).Result;

					if (approvalResult is null)
						return new ConnectionResponce(ConnectionStausCode.ComputerRejected, null, null, null);
				}
				finally
				{
					if (approvalResult is null)
						port.Dispose();
				}

				ProcessClientJoin(clientInformation, port, request);
				var body = new SuccessfulConnectionResultBody(port.Port);
				return new ConnectionResponce(ConnectionStausCode.Successful, null, JObject.FromObject(body), body.GetType().AssemblyQualifiedName);
			}
			else return new ConnectionResponce(ConnectionStausCode.NoFreePort, null, null, null);
		}

		private void ProcessClientJoin(ClientConnectionInformation information, RentedPort port, ConnectionRequest request)
		{
			var listener = new TcpListener(options.Interface, port.Port);
			listener.Start();

			var cts = new CancellationTokenSource();
			var sychRoot = new object();

			Task.Delay(options.ClientConnectTimeout).ContinueWith(_ => mainThreadDispatcher.DispatchTask(() =>
			{
				lock (sychRoot)
				{
					cts.Cancel();
				}
			}));

			listener.AcceptTcpClientAsync(cts.Token).AsTask().ContinueWith(clientTask =>
			{
				lock (sychRoot)
				{
					listener.Stop();

					if (clientTask.IsCompletedSuccessfully)
					{
						var client = clientTask.Result;

						logger.Log(LogLevel.Information, ClientJoinedID, "Client ({Login}[{IP}]) joined", request.Login, information.OriginalEndPoint);

						mainThreadDispatcher.DispatchTask(() =>
						{
							IRemoteProtocolAgent remote = new RemoteProtocolAgent(client, agentWorker, logger, bodyTypeResolver);
							remote.Start();
							agents.Add(remote.UniqueAgentId, new(information, port, remote));
						});
					}
					else
					{
						if (clientTask.Exception is not null)
							logger.Log(LogLevel.Error, ClientJoinFailID, clientTask.Exception, "Failed to join client ({Login}[{IP}])", request.Login, information.OriginalEndPoint);
						else logger.Log(LogLevel.Error, ClientJoinFailID, clientTask.Exception, "Failed to join client ({Login}[{IP}]). Reason: Timeout", request.Login, information.OriginalEndPoint);
					}
				}
			});

		}

		private void ProcessClientRejoin(ServerSideClientInternal information, IRemoteProtocolAgent agent)
		{
			var listener = new TcpListener(options.Interface, information.RentedPort.Port);
			listener.Start();

			var cts = new CancellationTokenSource();
			var sychRoot = new object();

			Task.Delay(options.ClientConnectTimeout).ContinueWith(_ => mainThreadDispatcher.DispatchTask(() =>
			{
				lock (sychRoot)
				{
					cts.Cancel();
					agent.Disconnect();
				}
			}));

			listener.AcceptTcpClientAsync(cts.Token).AsTask().ContinueWith(clientTask =>
			{
				lock (sychRoot)
				{
					listener.Stop();

					if (clientTask.IsCompletedSuccessfully)
					{
						var client = clientTask.Result;

						logger.Log(LogLevel.Information, ClientRejoinedID, "Client ({Login}[{IP}]) rejoined", information.ConnectionInformation.Login, agent.CurrentEndPoint);

						agent.Reconnect(client);
					}
					else
					{
						if (clientTask.Exception is not null)
							logger.Log(LogLevel.Error, ClientRejoinFailID, clientTask.Exception, "Failed to rejoin client ({Login}[{IP}])", information.ConnectionInformation.Login, agent.CurrentEndPoint);
						else logger.Log(LogLevel.Error, ClientRejoinFailID, clientTask.Exception, "Failed to rejoin client ({Login}[{IP}]). Reason: Timeout", information.ConnectionInformation.Login, agent.CurrentEndPoint);
					}
				}
			});

		}

		public void Close()
		{
			onServerClosed.Set();
		}

		public IEnumerable<ServerSideClient> ListClients()
		{
			return agents.Select(agent => new ServerSideClient(agent.Value.ConnectionInformation, agent.Value.ProtocolAgent)).ToArray();
		}

		public ServerSideClient GetClientByAgent(Guid protocolAgentId)
		{
			var client = agents[protocolAgentId];
			return new(client.ConnectionInformation, client.ProtocolAgent);
		}

		private record struct ServerSideClientInternal(ClientConnectionInformation ConnectionInformation, RentedPort RentedPort, IRemoteProtocolAgent ProtocolAgent);

		private class PortRentManager
		{
			private readonly PortRange ports;
			private readonly List<int> usedPorts;


			public PortRentManager(PortRange ports)
			{
				usedPorts = new(ports.Count);
				this.ports = ports;
			}


			public bool TryRentPort(out RentedPort port)
			{
				foreach (var maybePort in ports)
				{
					if (usedPorts.Contains(maybePort))
						continue;
					else
					{
						usedPorts.Add(maybePort);
						port = new(maybePort, new RentFinalizer(usedPorts, maybePort));
						return true;
					}
				}

				port = default;
				return false;
			}


			private class RentFinalizer : IDisposable
			{
				private readonly List<int> usedPorts;
				private readonly int portToDelete;


				public RentFinalizer(List<int> usedPorts, int portToDelete)
				{
					this.usedPorts = usedPorts;
					this.portToDelete = portToDelete;
				}


				public void Dispose()
				{
					usedPorts.Remove(portToDelete);
				}
			}
		}

		private record struct RentedPort(int Port, IDisposable RentFinalizer) : IDisposable
		{
			public void Dispose()
			{
				RentFinalizer.Dispose();
			}
		}

		private class AgentWorker : IRemoteAgentWorker
		{
			private readonly Server owner;


			public AgentWorker(Server owner)
			{
				this.owner = owner;
			}


			public void DispatchMessage(IRemoteProtocolAgent agent, ProtocolMessage message)
			{
				owner.logger.Log(LogLevel.Debug, MessageRecivedID, "New message recived from client ({Login}[{IP}])\n\t{Message}", owner.agents[agent.UniqueAgentId].ConnectionInformation.Login, agent.CurrentEndPoint, message);

				owner.mainThreadDispatcher.DispatchTask(() =>
				{
					owner.messageHandler.HandleMessageAsync(message, agent);
				});
			}

			public void HandleDisconnect(IRemoteProtocolAgent agent)
			{
				owner.logger.Log(LogLevel.Information, ClientDisconnectedID, "Client ({Login}[{IP}]) disconnected", owner.agents[agent.UniqueAgentId].ConnectionInformation.Login, agent.CurrentEndPoint);

				owner.mainThreadDispatcher.DispatchTask(() =>
				{
					owner.agents[agent.UniqueAgentId].RentedPort.Dispose();
					owner.agents.Remove(agent.UniqueAgentId);
				});
			}

			public void HandleError(IRemoteProtocolAgent agent, Exception ex)
			{
				owner.logger.Log(LogLevel.Error, ProtocolErrorID, ex, "Error in protocol agent for client ({Login}[{IP}])", owner.agents[agent.UniqueAgentId].ConnectionInformation.Login, agent.CurrentEndPoint);
			}

			public void ScheduleReconnect(IRemoteProtocolAgent agent)
			{
				owner.logger.Log(LogLevel.Information, ClientConnectionLostID, "Client ({Login}[{IP}]) connection lost", owner.agents[agent.UniqueAgentId].ConnectionInformation.Login, agent.CurrentEndPoint);

				owner.ProcessClientRejoin(owner.agents[agent.UniqueAgentId], agent);
			}
		}
	}
}
