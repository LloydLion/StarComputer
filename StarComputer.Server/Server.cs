using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using StarComputer.Shared;
using StarComputer.Shared.Interaction;
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
				var writer = new StreamWriter(stream);

				try
				{
					var responce = ProcessClient(stream);

					writer.WriteLine(JsonConvert.SerializeObject(responce, Formatting.None));
				}
				catch (Exception ex)
				{
					writer.WriteLine(JsonConvert.SerializeObject(new ConnectionResponce(ProtocolStausCode.ProtocolError, ex.ToString(), null), Formatting.None));
				}
			}
		}

		private ConnectionResponce ProcessClient(NetworkStream clientStream)
		{
			clientStream.ReadTimeout = 100;
			var reader = new StreamReader(clientStream);

			var json = reader.ReadLine();
			var request = JsonConvert.DeserializeObject<ConnectionRequest>(json ?? throw new NullReferenceException()) ?? throw new NullReferenceException();

			if (request.ServerPassword != options.ServerPassword)
				return new ConnectionResponce(ProtocolStausCode.InvalidPassword, request.ServerPassword, null);

			if (portRent.HasFreePort())
			{
				var isOk = clientApprovalAgent.ApproveClientAsync(new(request.Login, (IPEndPoint)(clientStream.Socket.RemoteEndPoint ?? throw new NullReferenceException()))).Result;

				if (isOk == false)
					return new ConnectionResponce(ProtocolStausCode.ComputerRejected, null, null);

				return new ConnectionResponce(ProtocolStausCode.Successful, null, new SuccessfulConnectionResultBody(portRent.RentPort()));
			}
			else return new ConnectionResponce(ProtocolStausCode.NoFreePort, null, null);
		}


		private class PortRentManager
		{
			private readonly PortRange ports;


			public PortRentManager(PortRange ports)
			{
				this.ports = ports;
			}


			public bool HasFreePort()
			{

			}

			public int RentPort()
			{

			}
		}
	}
}
