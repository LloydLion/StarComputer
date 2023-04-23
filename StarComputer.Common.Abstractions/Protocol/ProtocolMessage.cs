namespace StarComputer.Common.Abstractions.Protocol
{
	public class ProtocolMessage
	{
		public ProtocolMessage(string domain, object body, MessageAttachment? attachment, string? debugMessage)
		{
			TimeStamp = DateTime.UtcNow;
			Domain = domain;
			Body = body;
			Attachment = attachment;
			DebugMessage = debugMessage;
		}

		public ProtocolMessage(DateTime utcSendTime, string domain, object body, MessageAttachment? attachment, string? debugMessage)
		{
			TimeStamp = utcSendTime;
			Domain = domain;
			Body = body;
			Attachment = attachment;
			DebugMessage = debugMessage;
		}


		public DateTime TimeStamp { get; }

		public string Domain { get; }

		public object Body { get; }

		public MessageAttachment? Attachment { get; }

		public string? DebugMessage { get; }


		public record MessageAttachment(string Name, CopyToDelegate CopyDelegate, int Length);


		public override string ToString()
		{
			return $"ProtocolMessage (timeStamp: {TimeStamp.Ticks}, domain: {Domain}, debug: {DebugMessage ?? "No message"}, attachment: {Attachment?.Name ?? "No attachment"}) {SerializationContext.Instance.Serialize(Body)}";
		}
	}
}