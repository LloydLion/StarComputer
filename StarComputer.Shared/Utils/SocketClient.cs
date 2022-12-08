using Newtonsoft.Json;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace StarComputer.Shared.Utils
{
	public class SocketClient
	{
		private const int BytesChunkSize = 512;


		private readonly TcpClient client;
		private readonly NetworkStream stream;
		private readonly StreamWriter writer;
		private readonly StreamReader reader;


		public SocketClient(TcpClient client)
		{
			this.client = client;

			stream = client.GetStream();
			stream.ReadTimeout = StaticInformation.ClientMessageSendTimeout;
			stream.WriteTimeout = StaticInformation.ClientMessageSendTimeout;

			writer = new StreamWriter(stream, Encoding.UTF8) { AutoFlush = true, NewLine = "\n" };
			reader = new StreamReader(stream, Encoding.UTF8);

			ReadBytes(3); //Read utf-8 DOM bytes

			EndPoint = (IPEndPoint)(client.Client.RemoteEndPoint ?? throw new NullReferenceException("Remote endpoint was null"));
		}


		public bool IsDataAvailable => stream.DataAvailable;

		public bool IsConnected => client.Connected;

		public IPEndPoint EndPoint { get; }


		public void WriteJson<TObject>(TObject value) where TObject : notnull
		{
			var json = JsonConvert.SerializeObject(value);

			PrintColored(json, ConsoleColor.Green);
			writer.WriteLine(json);
		}

		public TObject ReadJson<TObject>() where TObject : notnull
		{
			var json = reader.ReadLine() ?? throw new NullReferenceException();
			
			PrintColored(json, ConsoleColor.Magenta);
			return JsonConvert.DeserializeObject<TObject>(json) ?? throw new NullReferenceException();
		}

		public void CopyFrom(CopyToDelegate copyTo)
		{
			copyTo(stream).AsTask().Wait();
		}

		public void Close() => client.Close();

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

			PrintColored($"--- Read {count} bytes ---", ConsoleColor.Magenta);

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

			PrintColored($"--- Wrote {bytes.Length} bytes ---", ConsoleColor.Green);
		}

		private static void PrintColored(string str, ConsoleColor color)
		{
			var tmp = Console.ForegroundColor;
			Console.ForegroundColor = color;
			Console.WriteLine(str);
			Console.ForegroundColor = tmp;
		}
	}
}
