namespace StarComputer.Client.Abstractions.Protocol.Utils;

public abstract class BinaryStreamReader
{
	public abstract long TotalLength { get; }

	public abstract long Position { get; }


	public abstract ValueTask CopyToAsync(Stream destination, CancellationToken cancellationToken = default);

	public abstract ValueTask CopyToAsync(Memory<byte> destination, CancellationToken cancellationToken = default);

	public abstract void Skip(int skipSize);

	public ValueTask ReadAsync(Memory<byte> destination, int offset, int length, CancellationToken cancellationToken = default) =>
		CopyToAsync(destination.Slice(offset, length), cancellationToken);


	//TODO: implement this methods
	public static BinaryStreamReader CreateFromDelegate(Func<Memory<byte>, ValueTask> reader, long totalLength)
	{
		throw new NotImplementedException();
	}

	public static BinaryStreamReader CreateFromData(ReadOnlyMemory<byte> data)
	{
		throw new NotImplementedException();
	}
}
