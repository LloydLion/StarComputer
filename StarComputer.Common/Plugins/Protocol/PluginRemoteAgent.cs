using StarComputer.Common.Abstractions.Plugins;
using StarComputer.Common.Abstractions.Plugins.Protocol;
using StarComputer.Common.Abstractions.Protocol;

namespace StarComputer.Common.Plugins.Protocol
{
	public class PluginRemoteAgent : IPluginRemoteAgent
	{
		private readonly IRemoteProtocolAgent agent;
		private readonly string targetPluginDomain;


		public PluginRemoteAgent(IRemoteProtocolAgent agent, PluginDomain targetPluginDomain)
		{
			this.agent = agent;
			this.targetPluginDomain = targetPluginDomain;
		}


		public Guid UniqueAgentId => agent.UniqueAgentId;


		public void Disconnect()
		{
			agent.Disconnect();
		}

		public Task SendMessageAsync(PluginProtocolMessage message)
		{
			var protocolMessage = new ProtocolMessage(targetPluginDomain, message.Body, message.Attachment is null ? null :
				new ProtocolMessage.MessageAttachment(message.Attachment.Name, message.Attachment.CopyDelegate, message.Attachment.Length), null);

			return agent.SendMessageAsync(protocolMessage);
		}


		public override bool Equals(object? obj)
		{
			return obj is PluginRemoteAgent other && other.UniqueAgentId == UniqueAgentId;
		}

		public override int GetHashCode()
		{
			return UniqueAgentId.GetHashCode();
		}
	}
}
