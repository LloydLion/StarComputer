﻿using StarComputer.Common.Abstractions.Plugins;
using StarComputer.Common.Abstractions.Plugins.Commands;
using StarComputer.Server.Abstractions;

namespace StarComputer.Server
{
	public class ServerPluginInitializer<TUI> : IPluginInitializer where TUI : IUIContext
	{
		private readonly IServer server;
		private readonly ICommandRepositoryBuilder builder;
		private readonly TUI ui;


		public ServerPluginInitializer(IServer server, ICommandRepositoryBuilder builder, TUI ui)
		{
			this.server = server;
			this.builder = builder;
			this.ui = ui;
		}


		public void InitializePlugins(IEnumerable<IPlugin> plugins)
		{
			foreach (var plugin in plugins)
			{
				if (plugin.TargetUIContextType.IsAssignableFrom(typeof(TUI)))
				{
					plugin.Initialize(new ServerProtocolEnvironment(server), ui);

					builder.BeginPluginInitalize(plugin);
					plugin.LoadCommands(builder);
					builder.EndPluginInitalize();
				}
			}
		}
	}
}
