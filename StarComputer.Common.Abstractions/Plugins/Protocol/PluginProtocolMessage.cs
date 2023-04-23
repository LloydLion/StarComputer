namespace StarComputer.Common.Abstractions.Plugins.Protocol
{
	public class PluginProtocolMessage
	{
		public PluginProtocolMessage(object body, MessageAttachment? attachment = null)
		{
			Body = body;
			Attachment = attachment;
		}


		public object Body { get; }

		public MessageAttachment? Attachment { get; }


		public record MessageAttachment(string Name, CopyToDelegate CopyDelegate, int Length);
	}
}