using System.Runtime.CompilerServices;

namespace StarComputer.Client.Abstractions.Protocol.Bundle;

[InlineArray(32)]
public struct BundleHash { private byte _element; }

public static class BundleHashExtensions
{
	public static void FillWith(this BundleHash self, ReadOnlySpan<byte> hashCode)
	{
		unsafe
		{
			void* ptr = &self;
			Unsafe.InitBlock(ptr, 0, 32); //Clear address
			var span = new Span<byte>(ptr, 32);
			hashCode.CopyTo(span);
		}
	}
}