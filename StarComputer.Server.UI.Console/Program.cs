using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using StarComputer.Server;
using StarComputer.Common.Utils.Logging;
using StarComputer.Server.Abstractions;
using StarComputer.Common.Abstractions.Plugins;
using StarComputer.Common.Abstractions.Protocol;
using StarComputer.Server.DebugEnv;

var services = new ServiceCollection()
	.Configure<ServerConfiguration>(config =>
	{

	})

	.AddSingleton<IServer, Server>()

	.AddSingleton<IMessageHandler, PluginOrientedMessageHandler>()
	.AddSingleton<IClientApprovalAgent, GugApprovalAgent>()
	.AddSingleton<IPluginInitializer, ServerPluginInitializer>()

	.AddLogging(builder => builder.SetMinimumLevel(LogLevel.Trace).AddFancyLogging())

	.BuildServiceProvider();


services.GetRequiredService<IPluginInitializer>().InitializePlugins(services.GetServices<IPlugin>());

services.GetRequiredService<IServer>().Listen();