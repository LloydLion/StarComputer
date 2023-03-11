using StarComputer.Common.Abstractions.Plugins.Protocol;

namespace StarComputer.Common.Abstractions.Plugins
{
	public record struct MessageContext(IPluginRemoteAgent Agent);
}
