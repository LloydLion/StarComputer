namespace StarComputer.Common.Abstractions.Plugins.Protocol
{
	public class PluginProtocolMessage
	{
		public PluginProtocolMessage(object? body, IEnumerable<Attachment>? attachments = null)
		{
			Body = body;
			Attachments = attachments?.ToDictionary(s => s.Name);
		}


		public object? Body { get; }

		public IReadOnlyDictionary<string, Attachment>? Attachments { get; }


		public record Attachment(string Name, CopyToDelegate CopyDelegate, int Length);
	}
}