using StarComputer.Common.Abstractions.Plugins.Protocol;

namespace StarComputer.Server.Abstractions.Plugins
{
	public interface IPluginServer
	{
		public event EventHandler<ServerPluginClientStatusChangedEventArgs> ClientConnected;

		public event EventHandler<ServerPluginClientStatusChangedEventArgs> ClientDisconnected;


		public IEnumerable<ServerSidePluginClient> ListClients();

		public ServerSidePluginClient GetClientByAgent(IPluginRemoteAgent protocolAgent);
	}
}
