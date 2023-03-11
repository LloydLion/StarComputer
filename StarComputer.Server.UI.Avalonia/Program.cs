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
		private static IServiceProvider? services;


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

			services = new ServiceCollection()
				.Configure<ClientConfiguration>(s => config.GetSection("Client").Bind(s))
				.Configure<FileResourcesCatalog.Options>(s => config.GetSection("Resources").Bind(s))
				.Configure<ReflectionPluginLoader.Options>(s =>
				{
					s.PluginDirectories = config.GetSection("PluginLoading:Reflection").GetValue<string>("PluginDirectories")!;
				})
				.Configure<HTMLUIManager.Options>(config.GetSection("HTMLPUI"))

				.AddSingleton<IServer, Server>()

				.AddTransient<IMessageHandler, PluginOrientedMessageHandler>()
				.AddSingleton<HTMLUIManager>()
				.AddTransient<IClientApprovalAgent, GugApprovalAgent>()
				.AddSingleton<IResourcesCatalog, FileResourcesCatalog>()

				.AddSingleton<IThreadDispatcher<Action>>(new ThreadDispatcher<Action>(Thread.CurrentThread, s => s()))

				.AddSingleton<IBodyTypeResolver, BodyTypeResolver>()

				.AddSingleton<IPluginLoader, ReflectionPluginLoader>()
				.AddSingleton<IPluginStore, PluginStore>()

				.AddLogging(builder => builder.SetMinimumLevel(config.GetValue<LogLevel>("Logging:MinLevel")).AddConsole().AddDebug())

				.BuildServiceProvider();


			var lservices = services;
			var server = lservices.GetRequiredService<IServer>();
			var uiManager = lservices.GetRequiredService<HTMLUIManager>();

			SynchronizationContext.SetSynchronizationContext(lservices.GetRequiredService<IThreadDispatcher<Action>>().CraeteSynchronizationContext(s => s));

			var plugins = lservices.GetRequiredService<IPluginStore>();
			var pluginLoader = lservices.GetRequiredService<IPluginLoader>();
			var bodyTypeResolverBuilder = new BodyTypeResolverBuilder();

			var pluginInitializer = new PluginInitializer(bodyTypeResolverBuilder);
			pluginInitializer.SetServices((sp, proto) =>
			{
				sp.Register<IHTMLUIContext>(uiManager.CreateContext(proto));

				var env = new ServerProtocolEnvironment(server, proto);
				sp.Register<IServerProtocolEnvironment>(env);
				sp.Register<IProtocolEnvironment>(env);
			});

			plugins.InitializeStoreAsync(pluginLoader, pluginInitializer).AsTask().Wait();

			bodyTypeResolverBuilder.BakeToResolver(lservices.GetRequiredService<IBodyTypeResolver>());


			var clientThread = new Thread(ClientThreadHandle);
			clientThread.Start();
			Thread.CurrentThread.Name = "Main UI Thread";


			var avaloniaAppBuider = BuildAvaloniaApp();

			avaloniaAppBuider.AfterSetup(avaloniaAppBuider =>
			{
				var avaloniaApp = (App)avaloniaAppBuider.Instance!;
				avaloniaApp.Setup(services!);
			});

			avaloniaAppBuider.StartWithClassicDesktopLifetime(args);
		}


		static void ClientThreadHandle()
		{
			Thread.CurrentThread.Name = "Server thread";

			var lservices = services!;
			var server = lservices.GetRequiredService<IServer>();
			var plugins = lservices.GetRequiredService<IPluginStore>();

			SynchronizationContext.SetSynchronizationContext(lservices.GetRequiredService<IThreadDispatcher<Action>>().CraeteSynchronizationContext(s => s));

			server.MainLoop(plugins);
		}

		static AppBuilder BuildAvaloniaApp()
		{
			return AppBuilder.Configure<App>()
				.UsePlatformDetect()
				.LogToTrace();
		}
	}
}