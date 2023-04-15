using Avalonia;
using Microsoft.Extensions.Configuration;
using System.IO;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using StarComputer.Client.Abstractions;
using StarComputer.Common.Abstractions.Plugins;
using StarComputer.Common.Abstractions.Protocol;
using StarComputer.Common.Threading;
using StarComputer.Common.Abstractions.Threading;
using StarComputer.Common.Plugins;
using StarComputer.Common.Plugins.Loading;
using StarComputer.Common.Abstractions.Plugins.Loading;
using StarComputer.Common.Protocol.Bodies;
using StarComputer.Common.Abstractions.Protocol.Bodies;
using System;
using System.Threading;
using ReactiveUI;
using StarComputer.Common.Abstractions.Plugins.UI.HTML;
using StarComputer.Common.Abstractions.Plugins.Resources;
using StarComputer.Common.Plugins.Resources;
using StarComputer.UI.Avalonia;
using StarComputer.Server.Abstractions;
using StarComputer.Server.DebugEnv;
using System.Threading.Tasks;
using StarComputer.Common.Plugins.Persistence;
using StarComputer.Common.Abstractions.Plugins.Persistence;

namespace StarComputer.Server.UI.Avalonia
{
	public static class Program
	{
		[STAThread]
		public static void Main(string[] args)
		{
			var config = new ConfigurationBuilder().SetBasePath(Directory.GetCurrentDirectory()).AddJsonFile(
#if DEBUG
			"config-dev.json"
#else
			"config.json"
#endif
			).Build();

			Console.WriteLine("Using configuration: " + config.GetDebugView());
			Console.WriteLine();

			var serverThread = new Thread(ServerThreadHandle)
			{
				Name = "Server thread"
			};

			var services = new ServiceCollection()
				.Configure<ClientConfiguration>(s => config.GetSection("Client").Bind(s))
				.Configure<FileResourcesCatalog.Options>(s => config.GetSection("Resources").Bind(s))
				.Configure<ReflectionPluginLoader.Options>(s =>
				{
					s.PluginDirectories = config.GetSection("PluginLoading:Reflection").GetValue<string>("PluginDirectories")!;
				})
				.Configure<HTMLUIManager.Options>(config.GetSection("HTMLPUI"))
				.Configure<FileBasedPluginPersistenceServiceProvider.Options>(s => config.GetSection("PluginPersistence").Bind(s))

				.AddSingleton<IServer, Server>()

				.AddTransient<IMessageHandler, PluginOrientedMessageHandler>()
				.AddTransient<IClientApprovalAgent, GugApprovalAgent>()
				.AddSingleton<IResourcesCatalog, FileResourcesCatalog>()
				.AddSingleton<IPluginPersistenceServiceProvider, FileBasedPluginPersistenceServiceProvider>()

				.AddSingleton<HTMLUIManager>()
				.AddSingleton<IBrowserCollection, BrowserCollection>()

				.AddSingleton<IThreadDispatcher<Action>>(new ThreadDispatcher<Action>(serverThread, s => s()))

				.AddSingleton<IBodyTypeResolver, BodyTypeResolver>()

				.AddSingleton<IPluginLoader, ReflectionPluginLoader>()
				.AddSingleton<IPluginStore, PluginStore>()

				.AddLogging(builder => builder.SetMinimumLevel(config.GetValue<LogLevel>("Logging:MinLevel")).AddConsole().AddDebug())

				.BuildServiceProvider();


			Thread.CurrentThread.Name = "Main UI Thread";

			var avaloniaAppBuider = BuildAvaloniaApp();

			avaloniaAppBuider.AfterSetup(avaloniaAppBuider =>
			{
				var avaloniaApp = (App)avaloniaAppBuider.Instance!;
				avaloniaApp.Setup(services, AvaloniaCallback, new { ServerThread = serverThread, Services = services });
			});

			avaloniaAppBuider.StartWithClassicDesktopLifetime(args);
		}

		private static void AvaloniaCallback(dynamic parameters)
		{
			IServiceProvider services = parameters.Services;
			Thread serverThread = parameters.ServerThread;

			var initializationTask = new AutoResetEvent(false);

			var targetSynchronizationContext = services.GetRequiredService<IThreadDispatcher<Action>>().CraeteSynchronizationContext(s => s);

			var server = services.GetRequiredService<IServer>();
			var uiManager = services.GetRequiredService<HTMLUIManager>();

			var plugins = services.GetRequiredService<IPluginStore>();
			var pluginLoader = services.GetRequiredService<IPluginLoader>();
			var bodyTypeResolverBuilder = new BodyTypeResolverBuilder();
			var pluginPersistenceServiceProvider = services.GetRequiredService<IPluginPersistenceServiceProvider>();

			var pluginInitializer = new PluginInitializer(bodyTypeResolverBuilder, targetSynchronizationContext);
			pluginInitializer.SetServices((sp, proto) =>
			{
				sp.Register<IHTMLUIContext>(uiManager.CreateContext(proto));
				sp.Register(pluginPersistenceServiceProvider.Provide(proto.Domain));

				var env = new ServerProtocolEnvironment(server, proto);
				sp.Register<IServerProtocolEnvironment>(env);
				sp.Register<IProtocolEnvironment>(env);
			});

			plugins.InitializeStore(pluginLoader, pluginInitializer);

			bodyTypeResolverBuilder.BakeToResolver(services.GetRequiredService<IBodyTypeResolver>());

			serverThread.Start(new
			{
				SynchronizationContext = targetSynchronizationContext,
				Services = services,
				InitializationTask = initializationTask
			});

			initializationTask.WaitOne();
		}

		private static void ServerThreadHandle(object? rawParameters)
		{
			dynamic parameters = rawParameters ?? throw new NullReferenceException();

			IServiceProvider services = parameters.Services;
			AutoResetEvent initializationTask = parameters.InitializationTask;

			SynchronizationContext.SetSynchronizationContext(services.GetRequiredService<IThreadDispatcher<Action>>().CraeteSynchronizationContext(s => s));
			var server = services.GetRequiredService<IServer>();
			var plugins = services.GetRequiredService<IPluginStore>();

			initializationTask.Set();
			server.MainLoop(plugins);
		}

		private static AppBuilder BuildAvaloniaApp()
		{
			return AppBuilder.Configure<App>()
				.UsePlatformDetect()
				.LogToTrace();
		}
	}
}
