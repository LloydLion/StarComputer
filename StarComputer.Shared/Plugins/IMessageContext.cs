using StarComputer.Shared.Protocol;

namespace StarComputer.Shared.Plugins
{
	public interface IMessageContext
	{
		public RemoteProtocolAgent Agent { get; }
	}
}
