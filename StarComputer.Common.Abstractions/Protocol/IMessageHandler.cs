namespace StarComputer.Common.Abstractions.Protocol
{
	public interface IMessageHandler
	{
		public Task HandleMessageAsync(ProtocolMessage message, IRemoteProtocolAgent agent);
	}
}
