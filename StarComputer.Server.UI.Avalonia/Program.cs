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

				.AddSingleton<IServer, Server>()

				.AddTransient<IMessageHandler, PluginOrientedMessageHandler>()
				.AddTransient<IClientApprovalAgent, GugApprovalAgent>()
				.AddSingleton<IResourcesCatalog, FileResourcesCatalog>()

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

			var cevent = new AutoResetEvent(false);

			var server = services.GetRequiredService<IServer>();
			var uiManager = services.GetRequiredService<HTMLUIManager>();

			var plugins = services.GetRequiredService<IPluginStore>();
			var pluginLoader = services.GetRequiredService<IPluginLoader>();
			var bodyTypeResolverBuilder = new BodyTypeResolverBuilder();

			var pluginInitializer = new PluginInitializer(bodyTypeResolverBuilder);
			pluginInitializer.SetServices((sp, proto) =>
			{
				sp.Register<IHTMLUIContext>(uiManager.CreateContext(proto));

				var env = new ServerProtocolEnvironment(server, proto);
				sp.Register<IServerProtocolEnvironment>(env);
				sp.Register<IProtocolEnvironment>(env);
			});

			plugins.InitializeStore(pluginLoader, pluginInitializer);

			bodyTypeResolverBuilder.BakeToResolver(services.GetRequiredService<IBodyTypeResolver>());

			serverThread.Start(new
			{
				Services = services,
				InitializationEvent = cevent,
				Server = server,
				Plugins = plugins
			});

			cevent.WaitOne();
		}

		private static void ServerThreadHandle(object? rawParameters)
		{
			dynamic parameters = rawParameters ?? throw new NullReferenceException();

			IServiceProvider services = parameters.Services;
			AutoResetEvent cevent = parameters.InitializationEvent;
			IServer server = parameters.Server;
			IPluginStore plugins = parameters.Plugins;

			SynchronizationContext.SetSynchronizationContext(services.GetRequiredService<IThreadDispatcher<Action>>().CraeteSynchronizationContext(s => s));

			cevent.Set();

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
