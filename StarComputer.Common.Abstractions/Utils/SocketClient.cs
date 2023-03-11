using Microsoft.Extensions.Logging;
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
		private readonly string name;
		private readonly NetworkStream stream;
		private readonly StreamWriter writer;
		private readonly StreamReader reader;


		public SocketClient(TcpClient client, ILogger logger, string name = "#")
		{
			this.client = client;
			this.logger = logger;
			this.name = name;
			stream = client.GetStream();
			stream.ReadTimeout = StaticInformation.ClientMessageSendTimeout;
			stream.WriteTimeout = StaticInformation.ClientMessageSendTimeout;

			writer = new StreamWriter(stream, Encoding.UTF8) { AutoFlush = true, NewLine = "\n" };
			reader = new StreamReader(stream, Encoding.UTF8);

			//Read UTF-8 BOM bytes
			Span<byte> nup = stackalloc byte[3];
			stream.Read(nup);

			EndPoint = (IPEndPoint)(client.Client.RemoteEndPoint ?? throw new NullReferenceException("Remote endpoint was null"));

			logger.Log(LogLevel.Debug, ClientCreatedID, "(SocketClient {Name}) New socket client created, endpoint {EndPoint}", name, EndPoint);
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


		public void WriteObject<TObject>(TObject value) where TObject : notnull
		{
			var data = SerializationContext.Instance.Serialize(value);

			writer.WriteLine(data);

			logger.Log(LogLevel.Trace, ObjectWroteID, "(SocketClient {Name}) New object wrote\n\t{Data}", name, data);
		}
		
		public void WriteObject(string serializedObject)
		{
			writer.WriteLine(serializedObject);

			logger.Log(LogLevel.Trace, ObjectWroteID, "(SocketClient {Name}) New object wrote\n\t{Data}", name, serializedObject);
		}

		public TObject ReadObject<TObject>() where TObject : notnull
		{
			var data = reader.ReadLine() ?? throw new NullReferenceException();

			logger.Log(LogLevel.Trace, ObjectRecivedID, "(SocketClient {Name}) New object recived\n\t{Data}", name, data);

			return SerializationContext.Instance.Deserialize<TObject>(data);
		}

		public string ReadObject()
		{
			var data = reader.ReadLine() ?? throw new NullReferenceException();

			logger.Log(LogLevel.Trace, ObjectRecivedID, "(SocketClient {Name}) New object recived\n\t{Data}", name, data);

			return data;
		}

		public void CopyFrom(CopyToDelegate copyTo)
		{
			copyTo(stream).AsTask().Wait();

			logger.Log(LogLevel.Trace, BinaryWroteID, "(SocketClient {Name}) Binary data wrote using CopyToDelegate, source: {Source}", name, copyTo.Method);
		}

		public void Close()
		{
			client.Close();

			logger.Log(LogLevel.Trace, ClientClosedDirectryID, "(SocketClient {Name}) Connection closed by internal command", name);
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

			logger.Log(LogLevel.Trace, BinaryRecivedID, "(SocketClient {Name}) Binary data recived, bytes length - {Length}", name, count);

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

			logger.Log(LogLevel.Trace, BinaryWroteID, "(SocketClient {Name}) Binary data wrote, bytes length - {Length}", name, bytes.Length);
		}
	}
}
