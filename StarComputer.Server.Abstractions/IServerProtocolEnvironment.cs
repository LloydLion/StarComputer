using StarComputer.Common.Abstractions.Plugins;
using StarComputer.Server.Abstractions.Plugins;

namespace StarComputer.Server.Abstractions
{
	public interface IServerProtocolEnvironment : IProtocolEnvironment
	{
		public IPluginServer Server { get; }
	}
}