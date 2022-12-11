namespace StarComputer.Shared.Protocol
{
	public interface IMessageHandler
	{
		public Task HandleMessageAsync(ProtocolMessage message, RemoteProtocolAgent agent);
	}
}
