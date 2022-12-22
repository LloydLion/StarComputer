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
using System.Net;
using Microsoft.Extensions.Configuration;
using StarComputer.Common.Plugins;


var config = new ConfigurationBuilder().SetBasePath(Directory.GetCurrentDirectory()).AddJsonFile(
#if DEBUG
	"config-dev.json"
#else
	"config.json"
#endif
	).Build();

Console.WriteLine("Using configuration: " + config.GetDebugView());
Console.WriteLine();

var services = new ServiceCollection()
	.Configure<ServerConfiguration>(s =>
	{
		config.GetSection("Server").Bind(s);
		s.Interface = IPAddress.Parse(config.GetSection("Server").GetValue<string>(nameof(s.Interface)) ?? throw new NullReferenceException());
	})
	.Configure<ReflectionPluginLoader.Options>(s => config.GetSection("PluginLoading:Reflection").Bind(s))

	.AddSingleton<IServer, Server>()

	.AddTransient<IMessageHandler, PluginOrientedMessageHandler>()
	.AddTransient<IClientApprovalAgent, GugApprovalAgent>()
	.AddTransient<IConsoleUIContext, ConsoleUIContext>()

	.AddSingleton<IThreadDispatcher<Action>>(new ThreadDispatcher<Action>(Thread.CurrentThread, s => s()))

	.AddSingleton<ICommandRepository, CommandRepository>()
	.AddSingleton<IPluginLoader, ReflectionPluginLoader>()
	.AddSingleton<IPluginStore, PluginStore>()

	.AddLogging(builder => builder.SetMinimumLevel(config.GetValue<LogLevel>("Logging:MinLevel")).AddFancyLogging())

	.BuildServiceProvider();


var server = services.GetRequiredService<IServer>();
var ui = services.GetRequiredService<IConsoleUIContext>();

SynchronizationContext.SetSynchronizationContext(services.GetRequiredService<IThreadDispatcher<Action>>().CraeteSynchronizationContext(s => s));

var plugins = services.GetRequiredService<IPluginStore>();
var pluginLoader = services.GetRequiredService<IPluginLoader>();
var commandRepositoryBuilder = new CommandRespositoryBuilder();
var pluginInitializer = new ServerPluginInitializer<IConsoleUIContext>(server, commandRepositoryBuilder, ui);

await plugins.InitializeStoreAsync(pluginLoader);
plugins.InitalizePlugins(pluginInitializer);
commandRepositoryBuilder.BakeToRepository(services.GetRequiredService<ICommandRepository>());

server.Listen(plugins);
