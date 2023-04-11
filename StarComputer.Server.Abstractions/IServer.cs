using StarComputer.Common.Abstractions.Plugins;

namespace StarComputer.Server.Abstractions
{
	public interface IServer
	{
		public bool IsListening { get; }


		public event Action ListeningStatusChanged;

		public event Action<ServerSideClient> ClientConnected;

		public event Action<ServerSideClient> ClientDisconnected;


		public ValueTask ListenAsync();

		public void Close();

		public IEnumerable<ServerSideClient> ListClients();

		public ServerSideClient GetClientByAgent(Guid protocolAgentId);

		public void MainLoop(IPluginStore plugins);
	}
}
