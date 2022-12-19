using StarComputer.Common.Abstractions.Plugins;

namespace StarComputer.Server.Abstractions
{
	public interface IServerProtocolEnvironment : IProtocolEnvironment
	{
		public IServer Server { get; }
	}
}