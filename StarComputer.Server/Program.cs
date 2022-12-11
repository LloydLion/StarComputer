using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using StarComputer.Server;
using StarComputer.Server.DebugEnv;
using StarComputer.Shared.Plugins;
using StarComputer.Shared.Protocol;
using StarComputer.Shared.Utils.Logging;

var services = new ServiceCollection()
	.Configure<ServerConfiguration>(config =>
	{

	})

	.AddSingleton<IServer, Server>()

	.AddSingleton<IMessageHandler, PluginOrientedMessageHandler>()
	.AddSingleton<IClientApprovalAgent, GugApprovalAgent>()
	.AddSingleton<IPluginInitializer, A>()

	.AddLogging(builder => builder.SetMinimumLevel(LogLevel.Trace).AddFancyLogging())

	.BuildServiceProvider();


services.GetRequiredService<IPluginInitializer>().InitializePlugins(services.GetServices<IPlugin>());

services.GetRequiredService<IServer>().Listen();