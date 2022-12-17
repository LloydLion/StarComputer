using System.Net;
using System.Net.Sockets;

namespace StarComputer.Common.Abstractions.Protocol
{
	public interface IRemoteProtocolAgent
	{
		IPEndPoint CurrentEndPoint { get; }

		void Disconnect();
		void Reconnect(TcpClient newClient);
		Task SendMessageAsync(ProtocolMessage message);
		void Start();
	}
}