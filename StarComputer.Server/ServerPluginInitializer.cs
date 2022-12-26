using StarComputer.Common.Abstractions.Plugins;
using StarComputer.Common.Abstractions.Plugins.Commands;
using StarComputer.Common.Abstractions.Protocol.Bodies;
using StarComputer.Server.Abstractions;

namespace StarComputer.Server
{
	public class ServerPluginInitializer<TUI> : IPluginInitializer where TUI : IUIContext
	{
		private readonly IServer server;
		private readonly ICommandRepositoryBuilder commandsBuilder;
		private readonly IBodyTypeResolverBuilder resolverBuilder;
		private readonly TUI ui;


		public ServerPluginInitializer(IServer server, ICommandRepositoryBuilder commandsBuilder, IBodyTypeResolverBuilder resolverBuilder, TUI ui)
		{
			this.server = server;
			this.commandsBuilder = commandsBuilder;
			this.resolverBuilder = resolverBuilder;
			this.ui = ui;
		}


		public void InitializePlugins(IEnumerable<IPlugin> plugins)
		{
			foreach (var plugin in plugins)
			{
				if (plugin.TargetUIContextType.IsAssignableFrom(typeof(TUI)))
				{
					resolverBuilder.SetupDomain(plugin.Domain);
					commandsBuilder.BeginPluginInitalize(plugin);

					plugin.InitializeAndBuild(new ServerProtocolEnvironment(server), ui, commandsBuilder, resolverBuilder);

					commandsBuilder.EndPluginInitalize();
				}
			}

			resolverBuilder.ResetDomain();
		}
	}
}
