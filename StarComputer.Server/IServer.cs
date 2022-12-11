using StarComputer.Shared.Protocol;

namespace StarComputer.Server
{
	public interface IServer
	{
		public void Listen();

		public void Close();

		public IEnumerable<ServerSideClient> ListClients();

		public ServerSideClient GetClientByAgent(RemoteProtocolAgent protocolAgent);
	}
}
