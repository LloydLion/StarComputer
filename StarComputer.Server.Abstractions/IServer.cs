using StarComputer.Common.Abstractions.Plugins;

namespace StarComputer.Server.Abstractions
{
	public interface IServer
	{
		public bool IsListening { get; }

		public bool IsCanStartListen { get; }


		public event EventHandler ListeningStatusChanged;

		public event EventHandler<ServerClientStatusChangedEventArgs> ClientConnected;

		public event EventHandler<ServerClientStatusChangedEventArgs> ClientDisconnected;


		public ValueTask ListenAsync();

		public ValueTask CloseAsync();

		public IEnumerable<ServerSideClient> ListClients();

		public ServerSideClient GetClientByAgent(Guid protocolAgentId);

		public ServerConfiguration GetConfiguration();

		public void MainLoop(IPluginStore plugins);
	}
}
