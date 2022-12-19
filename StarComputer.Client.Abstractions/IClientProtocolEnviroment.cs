using StarComputer.Common.Abstractions.Plugins;
using StarComputer.Common.Abstractions.Protocol;

namespace StarComputer.Client.Abstractions
{
	public interface IClientProtocolEnviroment : IProtocolEnvironment
	{
		public IClient Client { get; }
	}
}
