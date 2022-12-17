using StarComputer.Common.Abstractions.Protocol;

namespace StarComputer.Server.Abstractions
{
	public interface IServer
	{
		public void Listen();

		public void Close();

		public IEnumerable<ServerSideClient> ListClients();

		public ServerSideClient GetClientByAgent(IRemoteProtocolAgent protocolAgent);
	}
}
