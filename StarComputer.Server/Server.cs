using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;
using StarComputer.Shared;
using StarComputer.Shared.Connection;
using StarComputer.Shared.Protocol;
using StarComputer.Shared.Utils;
using System.Net.Sockets;

namespace StarComputer.Server
{
	internal class Server : IServer
	{
		private static readonly EventId ClientErrorID = new(11, "ClientError");
		

		private readonly TcpListener connectionListener;
		private readonly ServerConfiguration options;
		private readonly ILogger<Server> logger;
		private readonly IClientApprovalAgent clientApprovalAgent;
		private readonly PortRentManager portRent;

		private readonly Dictionary<RemoteProtocolAgent, RentedPort> agents = new();
		private readonly IMessageHandler messageHandler;
		private readonly AgentWorker agentWorker;

		private readonly ThreadDispatcher<Action> mainThreadDispatcher;


		public Server(IOptions<ServerConfiguration> options, ILogger<Server> logger, IClientApprovalAgent clientApprovalAgent, IMessageHandler messageHandler)
		{
			options.Value.Validate();

			this.options = options.Value;
			connectionListener = new TcpListener(options.Value.Interface, options.Value.ConnectionPort);
			this.logger = logger;
			this.clientApprovalAgent = clientApprovalAgent;
			this.messageHandler = messageHandler;
			portRent = new(options.Value.OperationsPortRange);
			agentWorker = new(this);

			mainThreadDispatcher = new(Thread.CurrentThread, s => s(), 1);
		}


		public void Listen()
		{
			connectionListener.Start(options.MaxPendingConnectionQueue);

			var waitForClient = new AutoResetEvent(false);
			Task<TcpClient> clientTask = connectionListener.AcceptTcpClientAsync().ContinueWith(s => { waitForClient.Set(); return s.Result; });

			SynchronizationContext.SetSynchronizationContext(mainThreadDispatcher.CraeteSynchronizationContext(s => s));

			while (true)
			{
				var index = mainThreadDispatcher.WaitHandlers(waitForClient);

				if (index == -1)
				{
					connectionListener.Stop();
					try { clientTask.Wait(); } catch (Exception) { }
					return;
				}

				if (index == 0)
				{
					var rawClient = clientTask.Result;
					clientTask = connectionListener.AcceptTcpClientAsync().ContinueWith(s => { waitForClient.Set(); return s.Result; });

					try
					{
						var client = new SocketClient(rawClient);

						try
						{
							var request = client.ReadJson<ConnectionRequest>();

							var responce = ProcessClientConnection(request, new(request.Login, client.EndPoint));

							client.WriteJson(responce);
						}
						catch (Exception ex)
						{
							client.WriteJson(new ConnectionResponce(ConnectionStausCode.ProtocolError, ex.ToString(), null, null));
							throw;
						}
					}
					catch (Exception ex)
					{
						logger.LogError(ex, "!!!");
					}
				}
				else if (index == -2)
				{
					try
					{
						mainThreadDispatcher.ExecuteAllTasks();
					}
					catch (Exception ex)
					{
						logger.LogError(ex, "$!!!");
					}
				}
			}
		}

		private ConnectionResponce ProcessClientConnection(ConnectionRequest request, ClientConnectionInformation clientInformation)
		{
			if (request.ProtocolVersion != options.TargetProtocolVersion)
				return new ConnectionResponce(ConnectionStausCode.IncompatibleVersion, $"Target version is {options.TargetProtocolVersion}", null, null);

			if (request.ServerPassword != options.ServerPassword)
				return new ConnectionResponce(ConnectionStausCode.InvalidPassword, request.ServerPassword, null, null);

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

				ProcessClientJoin(port);
				var body = new SuccessfulConnectionResultBody(port.Port);
				return new ConnectionResponce(ConnectionStausCode.Successful, null, JObject.FromObject(body), body.GetType().AssemblyQualifiedName);
			}
			else return new ConnectionResponce(ConnectionStausCode.NoFreePort, null, null, null);
		}

		private void ProcessClientJoin(RentedPort port)
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
					listener.Stop();
				}
			}));

			listener.AcceptTcpClientAsync(cts.Token).AsTask().ContinueWith(clientTask =>
			{
				lock (sychRoot)
				{
					if (clientTask.IsCompletedSuccessfully)
					{
						var client = clientTask.Result;
						listener.Stop();

						mainThreadDispatcher.DispatchTask(() =>
						{
							var remote = new RemoteProtocolAgent(client, agentWorker);
							remote.Start();
							agents.Add(remote, port);
						});
					}
				}
			});

		}

		private void ReprocessClientJoin(RentedPort port, RemoteProtocolAgent agent)
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
					listener.Stop();

					agent.Disconnect();
				}
			}));

			listener.AcceptTcpClientAsync(cts.Token).AsTask().ContinueWith(clientTask =>
			{
				lock (sychRoot)
				{
					if (clientTask.IsCompletedSuccessfully)
					{
						var client = clientTask.Result;
						listener.Stop();

						mainThreadDispatcher.DispatchTask(() => agent.Reconnect(client));
					}
				}
			});

		}


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


			public void DispatchMessage(RemoteProtocolAgent agent, ProtocolMessage message)
			{
				owner.mainThreadDispatcher.DispatchTask(() =>
				{
					owner.messageHandler.HandleMessageAsync(message, agent);
				});
			}

			public void HandleDisconnect(RemoteProtocolAgent agent)
			{
				owner.mainThreadDispatcher.DispatchTask(() =>
				{
					owner.agents[agent].Dispose();
					owner.agents.Remove(agent);
				});
			}

			public void HandleError(RemoteProtocolAgent agent, Exception ex)
			{
				owner.logger.LogError(ex, "Error in agent");
			}

			public void ScheduleReconnect(RemoteProtocolAgent agent)
			{
				owner.ReprocessClientJoin(owner.agents[agent], agent);
			}
		}
	}
}
