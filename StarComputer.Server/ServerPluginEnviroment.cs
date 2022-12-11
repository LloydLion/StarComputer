using StarComputer.Shared.Plugins;

namespace StarComputer.Server
{
	public class ServerPluginEnviroment : IPluginEnviroment
	{
		public ServerPluginEnviroment(IServer server)
		{
			Server = server;
		}


		public IServer Server { get; }
	}
}
