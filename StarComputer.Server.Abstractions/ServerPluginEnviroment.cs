using StarComputer.Common.Abstractions.Plugins;

namespace StarComputer.Server.Abstractions
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
