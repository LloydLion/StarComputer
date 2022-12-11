using StarComputer.Shared.Connection;
using StarComputer.Shared.Protocol;

namespace StarComputer.Server
{
	public record struct ServerSideClient(ClientConnectionInformation ConnectionInformation, RemoteProtocolAgent ProtocolAgent);
}
