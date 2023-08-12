using StarComputer.Client.Abstractions.Utils;

namespace StarComputer.Client.Abstractions;

public record Message(CopyToDelegate Content, Message.SendOptions Options)
{
	[Flags]
	public enum SendOptions
	{
		ShouldZip = 1 << 0
	}
}
