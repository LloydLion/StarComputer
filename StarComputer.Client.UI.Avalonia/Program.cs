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
using System.Net;
using StarComputer.Common.Abstractions.Plugins.UI.HTML;
using StarComputer.Common.Abstractions.Plugins.Resources;
using StarComputer.Common.Plugins.Resources;
using StarComputer.UI.Avalonia;
using Avalonia.Threading;
using StarComputer.Server.Abstractions;

namespace StarComputer.Client.UI.Avalonia
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

			var clientThread = new Thread(ClientThreadHandle)
			{
				Name = "Client thread"
			};

			var services = new ServiceCollection()
				.Configure<ClientConfiguration>(s => config.GetSection("Client").Bind(s))
				.Configure<FileResourcesCatalog.Options>(s => config.GetSection("Resources").Bind(s))
				.Configure<ReflectionPluginLoader.Options>(s =>
				{
					s.PluginDirectories = config.GetSection("PluginLoading:Reflection").GetValue<string>("PluginDirectories")!;
				})
				.Configure<ConnectionViewModel.Options>(s =>
				{
					var configSection = config.GetSection("Connection");
					s.IsConnectionDataLocked = configSection.GetValue<bool>("Locked");
					s.IsConnectionLoginLocked = configSection.GetValue<bool>("LoginLocked");

					s.InitialConnectionInformation = new(new IPEndPoint(
						IPAddress.Parse(
							configSection.GetValue<string>("IP")!),
							configSection.GetValue<int>("Port")
						),
						configSection.GetValue<string>("Password")!,
						configSection.GetValue<string>("Login")!);
				})
				.Configure<HTMLUIManager.Options>(config.GetSection("HTMLPUI"))

				.AddSingleton<IClient, Client>()

				.AddTransient<IMessageHandler, PluginOrientedMessageHandler>()
				.AddSingleton<IResourcesCatalog, FileResourcesCatalog>()

				.AddSingleton<HTMLUIManager>()
				.AddSingleton<IBrowserCollection, BrowserCollection>()

				.AddSingleton<IThreadDispatcher<Action>>(new ThreadDispatcher<Action>(clientThread, s => s()))
				
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
				avaloniaApp.Setup(services, AvaloniaCallback, new { ClientThread = clientThread, Services = services });
			});

			avaloniaAppBuider.StartWithClassicDesktopLifetime(args);
		}


		private static void AvaloniaCallback(dynamic parameters)
		{
			IServiceProvider services = parameters.Services;
			Thread clientThread = parameters.ClientThread;

			var cevent = new AutoResetEvent(false);

			var client = services.GetRequiredService<IClient>();
			var uiManager = services.GetRequiredService<HTMLUIManager>();

			var plugins = services.GetRequiredService<IPluginStore>();
			var pluginLoader = services.GetRequiredService<IPluginLoader>();
			var bodyTypeResolverBuilder = new BodyTypeResolverBuilder();

			var pluginInitializer = new PluginInitializer(bodyTypeResolverBuilder);
			pluginInitializer.SetServices((ps, proto) =>
			{
				ps.Register<IHTMLUIContext>(uiManager.CreateContext(proto));

				var env = new ClientProtocolEnvironment(client, proto);
				ps.Register<IClientProtocolEnviroment>(env);
				ps.Register<IProtocolEnvironment>(env);
			});

			plugins.InitializeStore(pluginLoader, pluginInitializer);

			bodyTypeResolverBuilder.BakeToResolver(services.GetRequiredService<IBodyTypeResolver>());

			clientThread.Start(new
			{
				Services = services,
				InitializationEvent = cevent,
				Client = client,
				Plugins = plugins
			});

			cevent.WaitOne();
		}

		private static void ClientThreadHandle(object? rawParameters)
		{
			dynamic parameters = rawParameters ?? throw new NullReferenceException();

			IServiceProvider services = parameters.Services;
			AutoResetEvent cevent = parameters.InitializationEvent;
			IClient client = parameters.Client;
			IPluginStore plugins = parameters.Plugins;

			SynchronizationContext.SetSynchronizationContext(services.GetRequiredService<IThreadDispatcher<Action>>().CraeteSynchronizationContext(s => s));

			cevent.Set();

			client.MainLoop(plugins);
		}

		private static AppBuilder BuildAvaloniaApp()
		{
			return AppBuilder.Configure<App>()
				.UsePlatformDetect()
				.LogToTrace();
		}
	}
}
