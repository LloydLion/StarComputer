using StarComputer.Common.Abstractions.Plugins.Protocol;

namespace StarComputer.Server.Abstractions.Plugins
{
	public interface IPluginServer
	{
		public event Action<ServerSidePluginClient> ClientConnected;

		public event Action<ServerSidePluginClient> ClientDisconnected;


		public IEnumerable<ServerSidePluginClient> ListClients();

		public ServerSidePluginClient GetClientByAgent(IPluginRemoteAgent protocolAgent);
	}
}
