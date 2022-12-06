using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using StarComputer.Shared;
using StarComputer.Shared.Connection;
using StarComputer.Shared.Protocol;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.Security;
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

		private readonly ConcurrentQueue<Action> dispathcedTasks = new();
		private readonly AutoResetEvent onTask = new(false);


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
		}


		public void Listen()
		{
			connectionListener.Start(10);

			var waitForClient = new AutoResetEvent(false);
			var toWait = new[] { waitForClient, onTask };

			Task<TcpClient> clientTask = connectionListener.AcceptTcpClientAsync().ContinueWith(s => { waitForClient.Set(); return s.Result; });

			while (true)
			{
				var index = WaitHandle.WaitAny(toWait);

				if (index == 0)
				{
					var client = clientTask.Result;
					clientTask = connectionListener.AcceptTcpClientAsync().ContinueWith(s => { waitForClient.Set(); return s.Result; });

					try
					{
						var stream = client.GetStream();

						stream.WriteTimeout = 100;
						stream.ReadTimeout = 100;
						var writer = new StreamWriter(stream) { AutoFlush = true };
						var reader = new StreamReader(stream);

						Thread.Sleep(250);

						try
						{
							var json = reader.ReadLine();
							var request = JsonConvert.DeserializeObject<ConnectionRequest>(json ?? throw new NullReferenceException()) ?? throw new NullReferenceException();

							var responce = ProcessClientConnection(request, new(request.Login, (IPEndPoint)(stream.Socket.RemoteEndPoint ?? throw new NullReferenceException())));

							writer.WriteLine(JsonConvert.SerializeObject(responce, Formatting.None));
						}
						catch (Exception ex)
						{
							writer.WriteLine(JsonConvert.SerializeObject(new ConnectionResponce(ConnectionStausCode.ProtocolError, ex.ToString(), null, null), Formatting.None));
						}
					}
					catch (Exception ex)
					{
						logger.LogError(ex, "!!!");
					}
				}
				else
				{
					try
					{
						while (dispathcedTasks.IsEmpty == false)
						{
							if (dispathcedTasks.TryDequeue(out var task))
							{
								try
								{
									task();
								}
								catch (Exception ex)
								{
									logger.LogError(ex, "#!!!");
								}
							}
						}
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
			Task.Delay(options.ClientConnectTimeout).ContinueWith(_ => EnqueueTask(() =>
			{
				cts.Cancel();
				listener.Stop();
			}));

			listener.AcceptTcpClientAsync(cts.Token).AsTask().ContinueWith(clientTask =>
			{
				var client = clientTask.Result;
				listener.Stop();

				EnqueueTask(() =>
				{
					var remote = new RemoteProtocolAgent(client, agentWorker, messageHandler);
					remote.Start();
					agents.Add(remote, port);
				});
			});

		}

		private void ReprocessClientJoin(RentedPort port, RemoteProtocolAgent agent)
		{
			var listener = new TcpListener(options.Interface, port.Port);
			listener.Start();

			var cts = new CancellationTokenSource();
			Task.Delay(options.ClientConnectTimeout).ContinueWith(_ => EnqueueTask(() =>
			{
				cts.Cancel();
				listener.Stop();
			}));

			listener.AcceptTcpClientAsync(cts.Token).AsTask().ContinueWith(clientTask =>
			{
				if (clientTask.IsCompletedSuccessfully)
				{
					var client = clientTask.Result;
					listener.Stop();
					agent.Reconnect(client);
				}
			});

		}

		private void EnqueueTask(Action task)
		{
			dispathcedTasks.Enqueue(task);
			onTask.Set();
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


			public void HandleDisconnect(RemoteProtocolAgent agent)
			{
				owner.EnqueueTask(() =>
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
