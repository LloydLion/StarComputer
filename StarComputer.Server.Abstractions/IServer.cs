using StarComputer.Common.Abstractions.Plugins;
using StarComputer.Common.Abstractions.Protocol;

namespace StarComputer.Server.Abstractions
{
	public interface IServer
	{
		public void Listen(IPluginStore plugins);

		public void Close();

		public IEnumerable<ServerSideClient> ListClients();

		public ServerSideClient GetClientByAgent(IRemoteProtocolAgent protocolAgent);
	}
}
