using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using StarComputer.Shared.Connection;
using StarComputer.Shared.Protocol;
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
			var client = new TcpClient();
			client.Connect(endPoint);

			var stream = client.GetStream();
			var writer = new StreamWriter(stream) { AutoFlush = true };
			var reader = new StreamReader(stream);

			writer.WriteLine(JsonConvert.SerializeObject(new ConnectionRequest(login, serverPassword, options.TargetProtocolVersion)));
			while (stream.DataAvailable == false) Thread.Sleep(10);
			var responce = JsonConvert.DeserializeObject<ConnectionResponce>(reader.ReadLine() ?? throw new NullReferenceException()) ?? throw new NullReferenceException();

			if (responce.DebugMessage is not null)
				Console.WriteLine("Debug: " + responce.DebugMessage);

			if (responce.StatusCode != ConnectionStausCode.Successful)
				throw new Exception($"Connection failed: {responce.StatusCode}");

			var bodyJson = responce.ResponceBody ?? throw new NullReferenceException();
			var body = bodyJson.ToObject<SuccessfulConnectionResultBody>() ?? throw new NullReferenceException();

			var port = body.ConnectionPort;

			client.Close();
			client.Dispose();

			//-----------------

			client = new TcpClient();
			client.Connect(new IPEndPoint(endPoint.Address, port));

			var onSomething = new AutoResetEvent(false);
			var agent = new Agent(onDisconnect);

			var remote = new RemoteProtocolAgent(client, agent, messageHandler);

			remote.Start();

			try
			{
				remote.SendMessageAsync(new ProtocolMessage("MAIN", "+", null, "Hello!")).Wait();
			}
			catch (Exception ex) { Console.WriteLine(ex); }

		reconnect:
			onSomething.WaitOne();

			if (agent.IsReconnecting)
			{
				client.Close();
				client = new TcpClient();
				client.Connect(new IPEndPoint(endPoint.Address, port));

				remote.Reconnect(client);

				goto reconnect;
			}
			else
			{
				Console.WriteLine("Done");
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
				IsReconnecting = false;
				onSomething.Set();
			}

			public void HandleError(RemoteProtocolAgent agent, Exception ex)
			{
				Console.WriteLine(ex.ToString());
			}

			public void ScheduleReconnect(RemoteProtocolAgent agent)
			{
				IsReconnecting = true;
				onSomething.Set();
			}


			public bool IsReconnecting { get; private set; }
		}
	}
}
