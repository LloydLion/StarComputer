using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using StarComputer.Shared;
using StarComputer.Shared.Interaction;
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


		public Server(IOptions<ServerConfiguration> options, ILogger<Server> logger, IClientApprovalAgent clientApprovalAgent)
		{
			this.options = options.Value;
			connectionListener = new TcpListener(options.Value.Interface, options.Value.ConnectionPort);
			this.logger = logger;
			this.clientApprovalAgent = clientApprovalAgent;
			portRent = new(options.Value.OperationsPortRange);
		}


		public void Listen()
		{
			connectionListener.Start(10);

			while (true)
			{
				var client = connectionListener.AcceptTcpClient();

				var stream = client.GetStream();

				stream.WriteTimeout = 100;
				stream.ReadTimeout = 100;
				var writer = new StreamWriter(stream);
				var reader = new StreamReader(stream);

				try
				{
					var json = reader.ReadLine();
					var request = JsonConvert.DeserializeObject<ConnectionRequest>(json ?? throw new NullReferenceException()) ?? throw new NullReferenceException();

					var responce = ProcessClient(request, new(request.Login, (IPEndPoint)(stream.Socket.RemoteEndPoint ?? throw new NullReferenceException())));

					writer.WriteLine(JsonConvert.SerializeObject(responce, Formatting.None));
				}
				catch (Exception ex)
				{
					writer.WriteLine(JsonConvert.SerializeObject(new ConnectionResponce(ConnectionStausCode.ProtocolError, ex.ToString(), null), Formatting.None));
				}
			}
		}

		private ConnectionResponce ProcessClient(ConnectionRequest request, ClientConnectionInformation clientInformation)
		{
			if (request.ProtocolVersion != options.TargetProtocolVersion)
				return new ConnectionResponce(ConnectionStausCode.IncompatibleVersion, $"Target version is {options.TargetProtocolVersion}", null);

			if (request.ServerPassword != options.ServerPassword)
				return new ConnectionResponce(ConnectionStausCode.InvalidPassword, request.ServerPassword, null);

			if (portRent.TryRentPort(out var port))
			{
				ClientApprovalResult? approvalResult = null;

				try
				{
					approvalResult = clientApprovalAgent.ApproveClientAsync(clientInformation).Result;

					if (approvalResult is null)
						return new ConnectionResponce(ConnectionStausCode.ComputerRejected, null, null);
				}
				finally
				{
					if (approvalResult is null)
						port.Dispose();
				}

				return new ConnectionResponce(ConnectionStausCode.Successful, null, new SuccessfulConnectionResultBody(port.Port));
			}
			else return new ConnectionResponce(ConnectionStausCode.NoFreePort, null, null);
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
	}
}
