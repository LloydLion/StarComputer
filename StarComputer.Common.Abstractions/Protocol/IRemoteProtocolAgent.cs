using System.Net;
using System.Net.Sockets;

namespace StarComputer.Common.Abstractions.Protocol
{
	public interface IRemoteProtocolAgent
	{
		public IPEndPoint CurrentEndPoint { get; }


		public void Disconnect();

		public void Reconnect(TcpClient newClient);

		public Task SendMessageAsync(ProtocolMessage message);

		public void Start();
	}
}