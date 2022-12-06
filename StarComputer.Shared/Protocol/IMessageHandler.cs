namespace StarComputer.Shared.Protocol
{
	public interface IMessageHandler
	{
		public Task<SendStatusCode> HandleMessageAsync(ProtocolMessage message, RemoteProtocolAgent agent);
	}
}
