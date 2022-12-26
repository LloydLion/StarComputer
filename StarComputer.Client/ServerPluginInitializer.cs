﻿using StarComputer.Common.Abstractions.Plugins;
using StarComputer.Common.Abstractions.Plugins.Commands;
using StarComputer.Client.Abstractions;
using StarComputer.Common.Abstractions.Protocol.Bodies;

namespace StarComputer.Client
{
	public class ClientPluginInitializer<TUI> : IPluginInitializer where TUI : IUIContext
	{
		private readonly IClient client;
		private readonly ICommandRepositoryBuilder commandsBuilder;
		private readonly IBodyTypeResolverBuilder resolverBuilder;
		private readonly TUI ui;


		public ClientPluginInitializer(IClient client, ICommandRepositoryBuilder commandsBuilder, IBodyTypeResolverBuilder resolverBuilder, TUI ui)
		{
			this.client = client;
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

					plugin.InitializeAndBuild(new ClientProtocolEnvironment(client), ui, commandsBuilder, resolverBuilder);

					commandsBuilder.EndPluginInitalize();
				}
			}

			resolverBuilder.ResetDomain();
		}
	}
}
