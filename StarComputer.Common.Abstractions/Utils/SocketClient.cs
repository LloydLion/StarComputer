using Microsoft.Extensions.Logging;
using System;
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


		private readonly TcpClient client;
		private readonly ILogger logger;
		private readonly string name;
		private readonly NetworkStream stream;
		private readonly List<byte> cacheList = new(200);


		public SocketClient(TcpClient client, ILogger logger, string name = "#")
		{
			this.client = client;
			this.logger = logger;
			this.name = name;
			stream = client.GetStream();
			stream.ReadTimeout = StaticInformation.ClientMessageSendTimeout;
			stream.WriteTimeout = StaticInformation.ClientMessageSendTimeout;

			EndPoint = (IPEndPoint)(client.Client.RemoteEndPoint ?? throw new NullReferenceException("Remote endpoint was null"));

			logger.Log(LogLevel.Debug, ClientCreatedID, "(SocketClient {Name}) New socket client created, endpoint {EndPoint}", name, EndPoint);
		}


		public bool IsDataAvailable => stream.DataAvailable;

		public int Available => stream.Socket.Available;

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

			stream.Write(Encoding.UTF8.GetBytes(data + '\n'));

			logger.Log(LogLevel.Trace, ObjectWroteID, "(SocketClient {Name}) New object wrote\n\t{Data}", name, data);
		}
		
		public void WriteObject(string serializedObject)
		{
			stream.Write(Encoding.UTF8.GetBytes(serializedObject + '\n'));

			logger.Log(LogLevel.Trace, ObjectWroteID, "(SocketClient {Name}) New object wrote\n\t{Data}", name, serializedObject);
		}

		public TObject ReadObject<TObject>() where TObject : notnull
		{
			var data = ReadObject();

			return SerializationContext.Instance.Deserialize<TObject>(data);
		}

		public string ReadObject()
		{
			cacheList.Clear();
			Span<byte> buffer = stackalloc byte[1];

			while (true)
			{
				stream.Read(buffer);
				if (buffer[0] == '\n')
					break;
				else cacheList.Add(buffer[0]);
			}

			var array = cacheList.ToArray();
			var data = Encoding.UTF8.GetString(array);
			cacheList.Clear();

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
			int timeout = 0;
			while (client.Available < count && ++timeout < 100)
				Thread.Sleep(1);

			var buffer = new byte[count];

			stream.Read(buffer);

			logger.Log(LogLevel.Trace, BinaryRecivedID, "(SocketClient {Name}) Binary data recived, bytes length - {Length}", name, count);

			return buffer.AsMemory();
		}

		public void WriteBytes(ReadOnlySpan<byte> bytes)
		{
			stream.Write(bytes);

			logger.Log(LogLevel.Trace, BinaryWroteID, "(SocketClient {Name}) Binary data wrote, bytes length - {Length}", name, bytes.Length);
		}
	}
}
