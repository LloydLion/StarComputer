using StarComputer.Common.Abstractions.Plugins.Protocol;

namespace StarComputer.Server.Abstractions.Plugins
{
	public interface IPluginServer
	{
		public IEnumerable<ServerSidePluginClient> ListClients();

		public ServerSidePluginClient GetClientByAgent(IPluginRemoteAgent protocolAgent);
	}
}
