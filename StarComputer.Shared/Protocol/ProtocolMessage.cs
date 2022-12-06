﻿namespace StarComputer.Shared.Protocol
{
	public class ProtocolMessage
	{
		public ProtocolMessage(string domain, object? body, IEnumerable<Attachment>? attachments, string? debugMessage)
		{
			TimeStamp = DateTime.UtcNow;
			Domain = domain;
			Body = body;
			Attachments = attachments?.ToDictionary(s => s.Name);
			DebugMessage = debugMessage;
		}

		public ProtocolMessage(DateTime utcSendTime, string domain, object? body, IEnumerable<Attachment>? attachments, string? debugMessage)
		{
			TimeStamp = utcSendTime;
			Domain = domain;
			Body = body;
			Attachments = attachments?.ToDictionary(s => s.Name);
			DebugMessage = debugMessage;
		}


		public DateTime TimeStamp { get; }

		public string Domain { get; }

		public object? Body { get; }

		public IReadOnlyDictionary<string, Attachment>? Attachments { get; }

		public string? DebugMessage { get; }


		public record Attachment(string Name, CopyToDelegate CopyDelegate, long Length);
	}
}