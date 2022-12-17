using StarComputer.Common.Abstractions.Plugins;
using StarComputer.Server.Abstractions;

namespace StarComputer.Server
{
	public class ServerPluginInitializer : IPluginInitializer
	{
		private readonly IServer server;


		public ServerPluginInitializer(IServer server)
		{
			this.server = server;
		}


		public void InitializePlugins(IEnumerable<IPlugin> plugins)
		{
			foreach (var plugin in plugins)
			{
				plugin.SetupEnviroment(new ServerPluginEnviroment(server));
			}
		}
	}
}
