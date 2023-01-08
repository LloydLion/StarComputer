using Avalonia;
using Microsoft.Extensions.Configuration;
using System.IO;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using StarComputer.Client.Abstractions;
using StarComputer.Common.Abstractions.Plugins;
using StarComputer.Common.Abstractions.Plugins.Commands;
using StarComputer.Common.Abstractions.Protocol;
using StarComputer.Common.Plugins.Commands;
using StarComputer.Common.Abstractions.Utils.Logging;
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
using System.Net;
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
				.Configure<ResourcesCatalog.Options>(s => config.GetSection("Resources").Bind(s))
				.Configure<ReflectionPluginLoader.Options>(s =>
				{
					s.PluginDirectories = config.GetSection("PluginLoading:Reflection").GetValue<string>("PluginDirectories")!;
				})

				.AddSingleton<IServer, Server>()

				.AddTransient<IMessageHandler, PluginOrientedMessageHandler>()
				.AddSingleton<HTMLUIManager>()
				.AddTransient<IClientApprovalAgent, GugApprovalAgent>()
				.AddSingleton<IResourcesCatalog, ResourcesCatalog>()

				.AddSingleton<IThreadDispatcher<Action>>(new ThreadDispatcher<Action>(Thread.CurrentThread, s => s()))

				.AddSingleton<ICommandRepository, CommandRepository>()
				.AddSingleton<IBodyTypeResolver, BodyTypeResolver>()

				.AddSingleton<ICommandRepository, CommandRepository>()
				.AddSingleton<IPluginLoader, ReflectionPluginLoader>()
				.AddSingleton<IPluginStore, PluginStore>()

				.AddLogging(builder => builder.SetMinimumLevel(config.GetValue<LogLevel>("Logging:MinLevel")).AddFancyLogging())

				.BuildServiceProvider();


			var lservices = services;
			var server = lservices.GetRequiredService<IServer>();
			var uiManager = lservices.GetRequiredService<HTMLUIManager>();

			SynchronizationContext.SetSynchronizationContext(lservices.GetRequiredService<IThreadDispatcher<Action>>().CraeteSynchronizationContext(s => s));

			var plugins = lservices.GetRequiredService<IPluginStore>();
			var pluginLoader = lservices.GetRequiredService<IPluginLoader>();
			var commandRepositoryBuilder = new CommandRespositoryBuilder();
			var bodyTypeResolverBuilder = new BodyTypeResolverBuilder();

			var pluginInitializer = new ServerPluginInitializer<IHTMLUIContext>(server, commandRepositoryBuilder, bodyTypeResolverBuilder, uiManager);

			plugins.InitializeStoreAsync(pluginLoader).AsTask().Wait();
			plugins.InitalizePlugins(pluginInitializer);

			commandRepositoryBuilder.BakeToRepository(lservices.GetRequiredService<ICommandRepository>());
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