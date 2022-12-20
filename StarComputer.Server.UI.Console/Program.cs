using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using StarComputer.Server;
using StarComputer.Common.Abstractions.Utils.Logging;
using StarComputer.Server.Abstractions;
using StarComputer.Common.Abstractions.Plugins;
using StarComputer.Common.Abstractions.Protocol;
using StarComputer.Server.DebugEnv;
using StarComputer.Common.Abstractions.Plugins.ConsoleUI;
using StarComputer.UI.Console.Plugins;
using StarComputer.Common.Plugins.Commands;
using StarComputer.Common.Abstractions.Plugins.Commands;
using StarComputer.Common.Abstractions.Threading;
using StarComputer.Common.Threading;

var services = new ServiceCollection()
	.Configure<ServerConfiguration>(config =>
	{

	})

	.AddSingleton<IServer, Server>()

	.AddTransient<IMessageHandler, PluginOrientedMessageHandler>()
	.AddTransient<IClientApprovalAgent, GugApprovalAgent>()
	.AddTransient<IConsoleUIContext, ConsoleUIContext>()

	.AddSingleton<IThreadDispatcher<Action>>(new ThreadDispatcher<Action>(Thread.CurrentThread, s => s()))

	.AddSingleton<ICommandRepository, CommandRepository>()

	.AddSingleton<IPlugin>(new HelloPlugin.HelloPlugin())

	.AddLogging(builder => builder.SetMinimumLevel(LogLevel.Trace).AddFancyLogging())

	.BuildServiceProvider();


SynchronizationContext.SetSynchronizationContext(services.GetRequiredService<IThreadDispatcher<Action>>().CraeteSynchronizationContext(s => s));

var builder = new CommandRespositoryBuilder();

var initializer = new ServerPluginInitializer<IConsoleUIContext>(services.GetRequiredService<IServer>(), builder, services.GetRequiredService<IConsoleUIContext>());
initializer.InitializePlugins(services.GetServices<IPlugin>());

builder.BakeToRepository(services.GetRequiredService<ICommandRepository>());


services.GetRequiredService<IServer>().Listen();