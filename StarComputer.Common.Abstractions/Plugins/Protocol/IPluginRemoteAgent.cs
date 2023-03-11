namespace StarComputer.Common.Abstractions.Plugins.Protocol
{
	public interface IPluginRemoteAgent
	{
		public Guid UniqueAgentId { get; }


		public void Disconnect();

		public Task SendMessageAsync(PluginProtocolMessage message);
	}
}
