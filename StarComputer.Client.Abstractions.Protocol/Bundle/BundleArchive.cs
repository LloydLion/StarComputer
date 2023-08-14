using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;

namespace StarComputer.Client.Abstractions.Protocol.Bundle;

public sealed class BundleArchive(Stream stream, ZipArchiveMode mode, bool leaveOpen = false, Encoding? entryNameEncoding = null) :
	ZipArchive(stream, mode, leaveOpen, entryNameEncoding)
{
	private readonly Stream _stream = stream;


	public async ValueTask<BundleHash> CalculateHashAsync(CancellationToken cancellationToken = default)
	{
		var md5 = MD5.Create();
		var hash = await md5.ComputeHashAsync(_stream, cancellationToken);

		var bundleHash = new BundleHash();
		bundleHash.FillWith(hash);

		return bundleHash;
	}
}
