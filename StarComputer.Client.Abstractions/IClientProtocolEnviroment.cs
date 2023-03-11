using StarComputer.Client.Abstractions.Plugins;
using StarComputer.Common.Abstractions.Plugins;

namespace StarComputer.Client.Abstractions
{
	public interface IClientProtocolEnviroment : IProtocolEnvironment
	{
		public IPluginClient Client { get; }
	}
}
