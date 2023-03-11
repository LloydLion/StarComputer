using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using StarComputer.Server;
using StarComputer.Server.Abstractions;
using StarComputer.Common.Abstractions.Plugins;
using StarComputer.Common.Abstractions.Protocol;
using StarComputer.Server.DebugEnv;
using StarComputer.Common.Abstractions.Plugins.UI.Console;
using StarComputer.UI.Console.Plugins;
using StarComputer.Common.Abstractions.Threading;
using StarComputer.Common.Threading;
using System.Net;
using Microsoft.Extensions.Configuration;
using StarComputer.Common.Plugins;
using StarComputer.Common.Plugins.Loading;
using StarComputer.Common.Abstractions.Plugins.Loading;
using StarComputer.Common.Protocol.Bodies;
using StarComputer.Common.Abstractions.Protocol.Bodies;
using StarComputer.Common.Abstractions.Plugins.Resources;
using StarComputer.Common.Plugins.Resources;

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
		s.Interface = IPAddress.Parse(config.GetSection("Server").GetValue<string>(nameof(s.Interface))!);
	})
	.Configure<ReflectionPluginLoader.Options>(s => s.PluginDirectories = config.GetSection("PluginLoading:Reflection").GetValue<string>("PluginDirectories")!)
	.Configure<FileResourcesCatalog.Options>(s => config.GetSection("Resources").Bind(s))

	.AddSingleton<IServer, Server>()

	.AddTransient<IMessageHandler, PluginOrientedMessageHandler>()
	.AddTransient<IClientApprovalAgent, GugApprovalAgent>()
	.AddTransient<IConsoleUIContext, ConsoleUIContext>()
	.AddSingleton<IResourcesCatalog, FileResourcesCatalog>()

	.AddSingleton<IThreadDispatcher<Action>>(new ThreadDispatcher<Action>(Thread.CurrentThread, s => s()))

	.AddSingleton<IBodyTypeResolver, BodyTypeResolver>()

	.AddSingleton<IPluginLoader, ReflectionPluginLoader>()
	.AddSingleton<IPluginStore, PluginStore>()

	.AddLogging(builder => builder.SetMinimumLevel(config.GetValue<LogLevel>("Logging:MinLevel")).AddConsole().AddDebug())

	.BuildServiceProvider();


var server = services.GetRequiredService<IServer>();
var ui = services.GetRequiredService<IConsoleUIContext>();

SynchronizationContext.SetSynchronizationContext(services.GetRequiredService<IThreadDispatcher<Action>>().CraeteSynchronizationContext(s => s));

var plugins = services.GetRequiredService<IPluginStore>();
var pluginLoader = services.GetRequiredService<IPluginLoader>();
var bodyTypeResolverBuilder = new BodyTypeResolverBuilder();

var pluginInitializer = new PluginInitializer(bodyTypeResolverBuilder);
pluginInitializer.SetServices((sp, proto) =>
{
	sp.Register(ui);

	var env = new ServerProtocolEnvironment(server, proto);
	sp.Register<IServerProtocolEnvironment>(env);
	sp.Register<IProtocolEnvironment>(env);
});

await plugins.InitializeStoreAsync(pluginLoader, pluginInitializer);

bodyTypeResolverBuilder.BakeToResolver(services.GetRequiredService<IBodyTypeResolver>());

server.ListenAsync().AsTask().Equals(null);
server.MainLoop(plugins);
