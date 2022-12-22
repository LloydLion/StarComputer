using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using StarComputer.Client;
using StarComputer.Client.Abstractions;
using StarComputer.Common.Abstractions;
using StarComputer.Common.Abstractions.Plugins;
using StarComputer.Common.Abstractions.Plugins.Commands;
using StarComputer.Common.Abstractions.Plugins.ConsoleUI;
using StarComputer.Common.Abstractions.Protocol;
using StarComputer.Common.Plugins.Commands;
using StarComputer.Common.Abstractions.Utils.Logging;
using StarComputer.UI.Console.Plugins;
using System.Net;
using StarComputer.Common.Threading;
using StarComputer.Common.Abstractions.Threading;
using StarComputer.Common.Plugins;
using Microsoft.Extensions.Configuration;


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
	.Configure<ClientConfiguration>(s => config.GetSection("Client").Bind(s))

	.AddSingleton<IClient, Client>()

	.AddTransient<IMessageHandler, PluginOrientedMessageHandler>()
	.AddTransient<IConsoleUIContext, ConsoleUIContext>()

	.AddSingleton<IThreadDispatcher<Action>>(new ThreadDispatcher<Action>(Thread.CurrentThread, s => s()))

	.AddSingleton<ICommandRepository, CommandRepository>()

	.AddSingleton<ICommandRepository, CommandRepository>()
	.AddSingleton<IPluginLoader, ReflectionPluginLoader>()
	.AddSingleton<IPluginStore, PluginStore>()

	.AddLogging(builder => builder.SetMinimumLevel(config.GetValue<LogLevel>("Logging:MinLevel")).AddFancyLogging())

	.BuildServiceProvider();


var client = services.GetRequiredService<IClient>();
var ui = services.GetRequiredService<IConsoleUIContext>();

SynchronizationContext.SetSynchronizationContext(services.GetRequiredService<IThreadDispatcher<Action>>().CraeteSynchronizationContext(s => s));

var plugins = services.GetRequiredService<IPluginStore>();
var pluginLoader = services.GetRequiredService<IPluginLoader>();
var commandRepositoryBuilder = new CommandRespositoryBuilder();
var pluginInitializer = new ClientPluginInitializer<IConsoleUIContext>(client, commandRepositoryBuilder, ui);

pluginInitializer.InitializePlugins(services.GetServices<IPlugin>());

await plugins.InitializeStoreAsync(pluginLoader);
plugins.InitalizePlugins(pluginInitializer);
commandRepositoryBuilder.BakeToRepository(services.GetRequiredService<ICommandRepository>());


Console.Write("Login: ");
var login = Console.ReadLine() ?? throw new NullReferenceException();

var connectionConfig = config.GetSection("Connection");
(var ip, var port, var password) = (connectionConfig.GetValue<string>("IP"), connectionConfig.GetValue<int>("Port"), connectionConfig.GetValue<string>("Password"));

client.Connect(new ConnectionConfiguration(new(IPAddress.Parse(ip ?? throw new NullReferenceException()), port), password ?? throw new NullReferenceException(), login), plugins);
