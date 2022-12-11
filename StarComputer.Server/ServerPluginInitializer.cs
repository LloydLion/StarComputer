using StarComputer.Shared.Plugins;

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
