using StarComputer.Client.Abstractions.Protocol.Utils;

namespace StarComputer.Client.Abstractions.Protocol;

public record Message(BinaryStreamReader Content, Message.SendOptions Options)
{
	[Flags]
	public enum SendOptions
	{
		ShouldZip = 1 << 0
	}
}
