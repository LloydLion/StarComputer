using System.Runtime.CompilerServices;

namespace StarComputer.Client.Abstractions.Machine;

[InlineArray(32)]
public struct MachineSessionToken { private byte _element; }


public static class MachineSessionTokenExtensions
{
	public static Guid GetVMA(this MachineSessionToken self)
	{
		unsafe
		{
			void* ptr = &self;
			var span = new ReadOnlySpan<byte>(ptr, 16);
			var guid = new Guid(span);
			return guid;
		}
	}
}
