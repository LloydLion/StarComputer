using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using StarComputer.Common.Abstractions;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace StarComputer.Common.Abstractions.Utils
{
	public class SocketClient
	{
		private static readonly EventId ClientCreatedID = new(90, "ClientCreated");
		private static readonly EventId ObjectWroteID = new(91, "ObjectWriten");
		private static readonly EventId ObjectRecivedID = new(92, "ObjectRecived");
		private static readonly EventId BinaryWroteID = new(93, "BinaryWriten");
		private static readonly EventId BinaryRecivedID = new(94, "BinaryRecived");
		private static readonly EventId ClientClosedDirectryID = new(95, "ClientClosedDirectry");
		private const int BytesChunkSize = 512;


		private readonly TcpClient client;
		private readonly ILogger logger;
		private readonly NetworkStream stream;
		private readonly StreamWriter writer;
		private readonly StreamReader reader;


		public SocketClient(TcpClient client, ILogger logger)
		{
			this.client = client;
			this.logger = logger;
			stream = client.GetStream();
			stream.ReadTimeout = StaticInformation.ClientMessageSendTimeout;
			stream.WriteTimeout = StaticInformation.ClientMessageSendTimeout;

			writer = new StreamWriter(stream, Encoding.UTF8) { AutoFlush = true, NewLine = "\n" };
			reader = new StreamReader(stream, Encoding.UTF8);

			//Read utf-8 BOM bytes
			Span<byte> nup = stackalloc byte[3];
			stream.Read(nup);

			EndPoint = (IPEndPoint)(client.Client.RemoteEndPoint ?? throw new NullReferenceException("Remote endpoint was null"));

			lock (logger)
			{
				using (logger.BeginScope("SocketClient {EndPoint}", EndPoint))
					logger.Log(LogLevel.Debug, ClientCreatedID, "New socket client created, endpoint {EndPoint}", EndPoint);
			}
		}


		public bool IsDataAvailable => stream.DataAvailable;

		public bool IsConnected
		{ 
			get
			{
				try
				{
					stream.Write(ReadOnlySpan<byte>.Empty);
					return true;
				}
				catch (Exception)
				{
					return false;
				}
			}
		}

		public IPEndPoint EndPoint { get; }


		public void WriteJson<TObject>(TObject value) where TObject : notnull
		{
			var json = JsonConvert.SerializeObject(value);

			writer.WriteLine(json);

			lock (logger)
			{
				using (logger.BeginScope("SocketClient {EndPoint}", EndPoint))
					logger.Log(LogLevel.Trace, ObjectWroteID, "New JSON object wrote\n\t{JSON}", json);
			}
		}

		public TObject ReadJson<TObject>() where TObject : notnull
		{
			var json = reader.ReadLine() ?? throw new NullReferenceException();

			lock (logger)
			{
				using (logger.BeginScope("SocketClient {EndPoint}", EndPoint))
					logger.Log(LogLevel.Trace, ObjectRecivedID, "New JSON object recived\n\t{JSON}", json);
			}

			return JsonConvert.DeserializeObject<TObject>(json) ?? throw new NullReferenceException();
		}

		public void CopyFrom(CopyToDelegate copyTo)
		{
			copyTo(stream).AsTask().Wait();

			lock (logger)
			{
				using (logger.BeginScope("SocketClient {EndPoint}", EndPoint))
					logger.Log(LogLevel.Trace, BinaryWroteID, "Binary data wrote using CopyToDelegate, source: {Source}", copyTo.Method);
			}
		}

		public void Close()
		{
			client.Close();

			lock (logger)
			{
				using (logger.BeginScope("SocketClient {EndPoint}", EndPoint))
					logger.Log(LogLevel.Trace, ClientClosedDirectryID, "Connection closed by internal command");
			}
		}

		public ReadOnlyMemory<byte> ReadBytes(int count)
		{
			var buffer = new byte[count];
			var span = buffer.AsSpan();

			for (int offset = 0; offset < count - BytesChunkSize; offset += BytesChunkSize)
			{
				var writeTo = span[..BytesChunkSize];
				stream.Read(writeTo);
				span = span[BytesChunkSize..];
			}

			stream.Read(span);

			lock (logger)
			{
				using (logger.BeginScope("SocketClient {EndPoint}", EndPoint))
					logger.Log(LogLevel.Trace, BinaryRecivedID, "Binary data recived, bytes length - {Length}", count);
			}

			return buffer.AsMemory();
		}

		public void WriteBytes(ReadOnlySpan<byte> bytes)
		{
			var span = bytes;

			for (int offset = 0; offset < bytes.Length - BytesChunkSize; offset += BytesChunkSize)
			{
				var writeFrom = span[..BytesChunkSize];
				stream.Write(writeFrom);
				span = span[BytesChunkSize..];
			}

			stream.Write(span);

			lock (logger)
			{
				using (logger.BeginScope("SocketClient {EndPoint}", EndPoint))
					logger.Log(LogLevel.Trace, BinaryWroteID, "Binary data wrote, bytes length - {Length}", bytes.Length);
			}
		}
	}
}
