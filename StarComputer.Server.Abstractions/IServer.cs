using StarComputer.Common.Abstractions.Plugins;
using StarComputer.Common.Abstractions.Protocol;

namespace StarComputer.Server.Abstractions
{
	public interface IServer
	{
		public bool IsListening { get; }


		public event Action ListeningStatusChanged;


		public ValueTask ListenAsync();

		public void Close();

		public IEnumerable<ServerSideClient> ListClients();

		public ServerSideClient GetClientByAgent(Guid protocolAgentId);

		public void MainLoop(IPluginStore plugins);
	}
}
