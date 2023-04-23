using System.Net;
using System.Net.Sockets;

namespace StarComputer.Common.Abstractions.Protocol
{
	public interface IRemoteProtocolAgent
	{
		public Guid UniqueAgentId { get; }

		public bool IsAlive { get; }


		public void Disconnect();

		public Task SendMessageAsync(ProtocolMessage message);

		public void Start();
	}
}