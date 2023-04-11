using StarComputer.Common.Abstractions.Plugins;
using StarComputer.Common.Abstractions.Plugins.Protocol;
using StarComputer.Common.Plugins.Protocol;
using StarComputer.Server.Abstractions;
using StarComputer.Server.Abstractions.Plugins;

namespace StarComputer.Server
{
	public class PluginServer : IPluginServer
	{
		private readonly IServer server;
		private readonly PluginDomain targetPluginDomain;
		private readonly ConnectionHandler connectionHandler;


		public PluginServer(IServer server, PluginDomain targetPluginDomain)
		{
			this.server = server;
			this.targetPluginDomain = targetPluginDomain;
			connectionHandler = new(server, targetPluginDomain);
		}


		public event Action<ServerSidePluginClient>? ClientConnected
		{ add => connectionHandler.ClientConnectHandler += value; remove => connectionHandler.ClientConnectHandler -= value; }

		public event Action<ServerSidePluginClient>? ClientDisconnected
		{ add => connectionHandler.ClientDisconnectHandler += value; remove => connectionHandler.ClientDisconnectHandler -= value; }


		public ServerSidePluginClient GetClientByAgent(IPluginRemoteAgent protocolAgent)
		{
			return new(server.GetClientByAgent(protocolAgent.UniqueAgentId).ConnectionInformation, protocolAgent);
		}

		public IEnumerable<ServerSidePluginClient> ListClients()
		{
			foreach (var client in server.ListClients())
				yield return new ServerSidePluginClient(client.ConnectionInformation, new PluginRemoteAgent(client.ProtocolAgent, targetPluginDomain));
		}


		private class ConnectionHandler
		{
			private readonly IServer server;
			private readonly PluginDomain targetPluginDomain;

			private Action<ServerSidePluginClient>? clientConnectHandler;
			private Action<ServerSidePluginClient>? clientDisconnectHandler;
			private bool isConnectedAttached = false;
			private bool isDisconnectedAttached = false;


			public Action<ServerSidePluginClient>? ClientConnectHandler { get => clientConnectHandler; set { clientConnectHandler = value; NotifyChanged(); } }

			public Action<ServerSidePluginClient>? ClientDisconnectHandler { get => clientDisconnectHandler; set { clientDisconnectHandler = value; NotifyChanged(); } }


			public ConnectionHandler(IServer server, PluginDomain targetPluginDomain)
			{
				this.server = server;
				this.targetPluginDomain = targetPluginDomain;
			}


			public void OnClientConnected(ServerSideClient client)
			{
				clientConnectHandler?.Invoke(new(client.ConnectionInformation, new PluginRemoteAgent(client.ProtocolAgent, targetPluginDomain)));
			}

			public void OnClientDisconnected(ServerSideClient client)
			{
				clientConnectHandler?.Invoke(new(client.ConnectionInformation, new PluginRemoteAgent(client.ProtocolAgent, targetPluginDomain)));
			}

			private void NotifyChanged()
			{
				if (isConnectedAttached == false && clientConnectHandler is not null)
				{
					isConnectedAttached = true;
					server.ClientConnected += OnClientConnected;
				}
				else if (isConnectedAttached == true && clientConnectHandler is null)
				{
					isConnectedAttached = false;
					server.ClientConnected -= OnClientConnected;
				}

				if (isDisconnectedAttached == false && clientDisconnectHandler is not null)
				{
					isDisconnectedAttached = true;
					server.ClientDisconnected += OnClientDisconnected;
				}
				else if (isDisconnectedAttached == true && clientDisconnectHandler is null)
				{
					isDisconnectedAttached = false;
					server.ClientDisconnected -= OnClientDisconnected;
				}
			}
		}
	}
}