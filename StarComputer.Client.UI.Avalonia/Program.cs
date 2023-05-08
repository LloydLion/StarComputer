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
using System.Threading.Tasks;
using StarComputer.Common.Plugins.Persistence;
using StarComputer.Common.Abstractions.Plugins.Persistence;
using StarComputer.Common.Abstractions;
using StarComputer.ApplicationUtils.Localization;
using Microsoft.Extensions.Localization;
using System.Linq;

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
				.Configure<ClientConfiguration>(s =>
				{
					var configSection = config.GetSection("Client");
					s.ClientHttpAddressTemplate = configSection.GetValue<string>("ClientHttpAddressTemplate") ?? StaticInformation.ClientHttpAddressTemplate;
					var connectionInterface = configSection.GetValue<string>("Interface");
					if (connectionInterface is not null) s.Interface = IPEndPoint.Parse(connectionInterface);
				})
				.Configure<FileResourcesCatalog.Options>(s => config.GetSection("Resources").Bind(s))
				.Configure<ReflectionPluginLoader.Options>(s =>
				{
					s.PluginDirectories = config.GetSection("PluginLoading:Reflection").GetValue<string>("PluginDirectories")!;
				})
				.Configure<ConnectionDialogViewModel.Options>(s =>
				{
					var configSection = config.GetSection("Connection");
					s.IsConnectionDataLocked = configSection.GetValue<bool>("Locked");
					s.IsConnectionLoginLocked = configSection.GetValue<bool>("LoginLocked");

					s.InitialConnectionInformation = new(new IPEndPoint(
							IPAddress.Parse(configSection.GetValue<string>("IP")!),
							configSection.GetValue<int>("Port")
						),
						configSection.GetValue<string>("Password")!,
						configSection.GetValue<string>("Login")!)
					{
						ServerHttpAddressTemplate = configSection.GetValue<string>("ServerHttpAddressTemplate") ?? StaticInformation.ServerHttpAddressTemplate
					};
				})
				.Configure<HTMLUIManager.Options>(config.GetSection("HTMLPUI"))
				.Configure<FileBasedPluginPersistenceServiceProvider.Options>(s => config.GetSection("PluginPersistence").Bind(s))

				.AddSingleton<IClient, Client>()

				.AddTransient<IMessageHandler, PluginOrientedMessageHandler>()
				.AddSingleton<IResourcesCatalog, FileResourcesCatalog>()
				.AddSingleton<IPluginPersistenceServiceProvider, FileBasedPluginPersistenceServiceProvider>()

				.AddSingleton<HTMLUIManager>()
				.AddSingleton<IBrowserCollection, BrowserCollection>()

				.AddSingleton<IThreadDispatcher<Action>>(new ThreadDispatcher<Action>(clientThread, s => s()))
				
				.AddSingleton<IBodyTypeResolver, BodyTypeResolver>()

				.AddSingleton<IPluginLoader, ReflectionPluginLoader>()
				.AddSingleton<IPluginStore, PluginStore>()

				.AddLogging(builder => builder.SetMinimumLevel(config.GetValue<LogLevel>("Logging:MinLevel")).AddConsole().AddDebug())

				.AddLocalization(options => { options.ResourcesPath = "Translations"; }, new[] { "StarComputer.UI.Avalonia" })

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

			var initializationTask = new AutoResetEvent(false);

			var targetSynchronizationContext = services.GetRequiredService<IThreadDispatcher<Action>>().CraeteSynchronizationContext(s => s);

			var client = services.GetRequiredService<IClient>();
			var uiManager = services.GetRequiredService<HTMLUIManager>();

			var plugins = services.GetRequiredService<IPluginStore>();
			var pluginLoader = services.GetRequiredService<IPluginLoader>();
			var bodyTypeResolverBuilder = new BodyTypeResolverBuilder();
			var pluginPersistenceServiceProvider = services.GetRequiredService<IPluginPersistenceServiceProvider>();
			var localizationFactory = services.GetRequiredService<IStringLocalizerFactory>();

			var pluginInitializer = new PluginInitializer(bodyTypeResolverBuilder, targetSynchronizationContext);
			pluginInitializer.SetServices((ps, proto) =>
			{
				ps.Register(localizationFactory.Create(proto.PluginType));
				ps.Register<IHTMLUIContext>(uiManager.CreateContext(proto));
				ps.Register(pluginPersistenceServiceProvider.Provide(proto.Domain));

				var env = new ClientProtocolEnvironment(client, proto);
				ps.Register<IClientProtocolEnviroment>(env);
				ps.Register<IProtocolEnvironment>(env);
			});

			plugins.InitializeStore(pluginLoader, pluginInitializer);

			bodyTypeResolverBuilder.BakeToResolver(services.GetRequiredService<IBodyTypeResolver>());

			clientThread.Start(new
			{
				SynchronizationContext = targetSynchronizationContext,
				Services = services,
				InitializationTask = initializationTask
			});

			initializationTask.WaitOne();
		}

		private static void ClientThreadHandle(object? rawParameters)
		{
			dynamic parameters = rawParameters ?? throw new NullReferenceException();

			IServiceProvider services = parameters.Services;
			AutoResetEvent initializationTask = parameters.InitializationTask;
			SynchronizationContext synchronizationContext = parameters.SynchronizationContext;

			SynchronizationContext.SetSynchronizationContext(synchronizationContext);
			var client = services.GetRequiredService<IClient>();
			var plugins = services.GetRequiredService<IPluginStore>();

			var joinKeys = new JoinKeyCollection();
			var pluginsInforamtion = plugins.Select(s => new { Domain = s.Key, Version = s.Value.Version.ToString() }).ToArray();
			joinKeys.Add(new JoinKey("plugins", pluginsInforamtion));

			initializationTask.Set();
			client.MainLoop(joinKeys);
		}

		private static AppBuilder BuildAvaloniaApp()
		{
			return AppBuilder.Configure<App>()
				.UsePlatformDetect()
				.LogToTrace();
		}
	}
}
