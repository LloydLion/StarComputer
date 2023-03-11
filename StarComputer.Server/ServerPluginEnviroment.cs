using StarComputer.Common.Abstractions.Plugins.Loading;
using StarComputer.Server.Abstractions;
using StarComputer.Server.Abstractions.Plugins;

namespace StarComputer.Server
{
	public class ServerProtocolEnvironment : IServerProtocolEnvironment
	{
		public ServerProtocolEnvironment(IServer server, PluginLoadingProto loadingProto)
		{
			Server = new PluginServer(server, loadingProto.Domain);
		}


		public IPluginServer Server { get; }
	}
}
