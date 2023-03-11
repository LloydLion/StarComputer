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


		public PluginServer(IServer server, PluginDomain targetPluginDomain)
		{
			this.server = server;
			this.targetPluginDomain = targetPluginDomain;
		}


		public ServerSidePluginClient GetClientByAgent(IPluginRemoteAgent protocolAgent)
		{
			return new(server.GetClientByAgent(protocolAgent.UniqueAgentId).ConnectionInformation, protocolAgent);
		}

		public IEnumerable<ServerSidePluginClient> ListClients()
		{
			foreach (var client in server.ListClients())
				yield return new ServerSidePluginClient(client.ConnectionInformation, new PluginRemoteAgent(client.ProtocolAgent, targetPluginDomain));
		}
	}
}