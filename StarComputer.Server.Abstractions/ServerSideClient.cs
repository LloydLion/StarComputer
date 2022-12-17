using StarComputer.Common.Abstractions.Connection;
using StarComputer.Common.Abstractions.Protocol;

namespace StarComputer.Server.Abstractions
{
	public record struct ServerSideClient(ClientConnectionInformation ConnectionInformation, IRemoteProtocolAgent ProtocolAgent);
}
