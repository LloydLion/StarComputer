using StarComputer.Common.Abstractions.Plugins;
using StarComputer.Common.Abstractions.Plugins.Commands;
using StarComputer.Client.Abstractions;

namespace StarComputer.Client
{
	public class ClientPluginInitializer<TUI> : IPluginInitializer where TUI : IUIContext
	{
		private readonly IClient client;
		private readonly ICommandRepositoryBuilder builder;
		private readonly TUI ui;


		public ClientPluginInitializer(IClient client, ICommandRepositoryBuilder builder, TUI ui)
		{
			this.client = client;
			this.builder = builder;
			this.ui = ui;
		}


		public void InitializePlugins(IEnumerable<IPlugin> plugins)
		{
			foreach (var plugin in plugins)
			{
				if (plugin.TargetUIContextType.IsAssignableFrom(typeof(TUI)))
				{
					plugin.Initialize(new ClientProtocolEnvironment(client), ui);

					builder.BeginPluginInitalize(plugin);
					plugin.LoadCommands(builder);
					builder.EndPluginInitalize();
				}
			}
		}
	}
}
