using StarComputer.Common.Abstractions.Connection;
using StarComputer.Common.Abstractions.Plugins.Protocol;

namespace StarComputer.Server.Abstractions.Plugins
{
	public record struct ServerSidePluginClient(ClientConnectionInformation ConnectionInformation, IPluginRemoteAgent Agent);
}
