using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using StarComputer.Client;
using StarComputer.Client.Abstractions;
using StarComputer.Common.Abstractions;
using StarComputer.Common.Abstractions.Plugins;
using StarComputer.Common.Abstractions.Plugins.UI.Console;
using StarComputer.Common.Abstractions.Protocol;
using StarComputer.UI.Console.Plugins;
using System.Net;
using StarComputer.Common.Threading;
using StarComputer.Common.Abstractions.Threading;
using StarComputer.Common.Plugins;
using Microsoft.Extensions.Configuration;
using StarComputer.Common.Plugins.Loading;
using StarComputer.Common.Abstractions.Plugins.Loading;
using StarComputer.Common.Protocol.Bodies;
using StarComputer.Common.Abstractions.Protocol.Bodies;
using StarComputer.Common.Plugins.Resources;
using StarComputer.Common.Abstractions.Plugins.Resources;
using StarComputer.Common.Abstractions.Plugins.Persistence;
using StarComputer.Common.Plugins.Persistence;

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
	.Configure<FileResourcesCatalog.Options>(s => config.GetSection("Resources").Bind(s))
	.Configure<ReflectionPluginLoader.Options>(s =>
	{
		s.PluginDirectories = config.GetSection("PluginLoading:Reflection").GetValue<string>("PluginDirectories")!;
	})
	.Configure<FileBasedPluginPersistenceServiceProvider.Options>(s => config.GetSection("PluginPersistence").Bind(s))

	.AddSingleton<IClient, Client>()

	.AddTransient<IMessageHandler, PluginOrientedMessageHandler>()
	.AddTransient<IConsoleUIContext, ConsoleUIContext>()
	.AddSingleton<IResourcesCatalog, FileResourcesCatalog>()
	.AddSingleton<IPluginPersistenceServiceProvider, FileBasedPluginPersistenceServiceProvider>()

	.AddSingleton<IThreadDispatcher<Action>>(new ThreadDispatcher<Action>(Thread.CurrentThread, s => s()))

	.AddSingleton<IBodyTypeResolver, BodyTypeResolver>()

	.AddSingleton<IPluginLoader, ReflectionPluginLoader>()
	.AddSingleton<IPluginStore, PluginStore>()

	.AddLogging(builder => builder.SetMinimumLevel(config.GetValue<LogLevel>("Logging:MinLevel")).AddConsole().AddDebug())

	.BuildServiceProvider();


var client = services.GetRequiredService<IClient>();
var ui = services.GetRequiredService<IConsoleUIContext>();

SynchronizationContext.SetSynchronizationContext(services.GetRequiredService<IThreadDispatcher<Action>>().CraeteSynchronizationContext(s => s));

var plugins = services.GetRequiredService<IPluginStore>();
var pluginLoader = services.GetRequiredService<IPluginLoader>();
var bodyTypeResolverBuilder = new BodyTypeResolverBuilder();
var pluginPersistenceServiceProvider = services.GetRequiredService<IPluginPersistenceServiceProvider>();

var pluginInitializer = new PluginInitializer(bodyTypeResolverBuilder);
pluginInitializer.SetServices((ps, proto) =>
{
	ps.Register(ui);
	ps.Register(pluginPersistenceServiceProvider.Provide(proto.Domain));

	var env = new ClientProtocolEnvironment(client, proto);
	ps.Register<IClientProtocolEnviroment>(env);
	ps.Register<IProtocolEnvironment>(env);
});

plugins.InitializeStore(pluginLoader, pluginInitializer);

bodyTypeResolverBuilder.BakeToResolver(services.GetRequiredService<IBodyTypeResolver>());


Console.Write("Login: ");
var login = Console.ReadLine()!;

var connectionConfig = config.GetSection("Connection");
(var ip, var port, var password) = (connectionConfig.GetValue<string>("IP"), connectionConfig.GetValue<int>("Port"), connectionConfig.GetValue<string>("Password"));

client.ConnectAsync(new ConnectionConfiguration(new(IPAddress.Parse(ip!), port), password!, login)).AsTask().Equals(null);
client.MainLoop(plugins);
