using StarComputer.Common.Abstractions.Protocol;

namespace StarComputer.Common.Abstractions.Plugins
{
	public interface IMessageContext
	{
		public IRemoteProtocolAgent Agent { get; }
	}
}
