using Avalonia;
using Microsoft.Extensions.Configuration;
using System.IO;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using StarComputer.Client;
using StarComputer.Client.Abstractions;
using StarComputer.Common.Abstractions.Plugins;
using StarComputer.Common.Abstractions.Plugins.Commands;
using StarComputer.Common.Abstractions.Plugins.ConsoleUI;
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
using System.Threading.Tasks;
using Avalonia.Controls;
using ReactiveUI;

namespace StarComputer.Client.UI.Avalonia
{
	public static class Program
	{
		private static IServiceProvider? services;
		private static string[]? args;


		[STAThread]
		public static void Main(string[] args)
		{
			Program.args = args;

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
				.Configure<ReflectionPluginLoader.Options>(s =>
				{
					s.PluginDirectories = config.GetSection("PluginLoading:Reflection").GetValue<string>("PluginDirectories")!;
				})

				.AddSingleton<IClient, Client>()

				.AddTransient<IMessageHandler, PluginOrientedMessageHandler>()
				.AddSingleton<IConsoleUIContext>(sp => sp.GetRequiredService<AvaloniaBasedConsoleUIContext>())
				.AddSingleton<AvaloniaBasedConsoleUIContext>()

				.AddSingleton<IThreadDispatcher<Action>>(new ThreadDispatcher<Action>(Thread.CurrentThread, s => s()))

				.AddSingleton<ICommandRepository, CommandRepository>()
				.AddSingleton<IBodyTypeResolver, BodyTypeResolver>()

				.AddSingleton<ICommandRepository, CommandRepository>()
				.AddSingleton<IPluginLoader, ReflectionPluginLoader>()
				.AddSingleton<IPluginStore, PluginStore>()

				.AddLogging(builder => builder.SetMinimumLevel(config.GetValue<LogLevel>("Logging:MinLevel")).AddFancyLogging())

				.BuildServiceProvider();


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
			Thread.CurrentThread.Name = "Client thread";

			var lservices = services!;
			var client = lservices.GetRequiredService<IClient>();
			var ui = lservices.GetRequiredService<IConsoleUIContext>();

			SynchronizationContext.SetSynchronizationContext(lservices.GetRequiredService<IThreadDispatcher<Action>>().CraeteSynchronizationContext(s => s));

			var plugins = lservices.GetRequiredService<IPluginStore>();
			var pluginLoader = lservices.GetRequiredService<IPluginLoader>();
			var commandRepositoryBuilder = new CommandRespositoryBuilder();
			var bodyTypeResolverBuilder = new BodyTypeResolverBuilder();

			var pluginInitializer = new ClientPluginInitializer<IConsoleUIContext>(client, commandRepositoryBuilder, bodyTypeResolverBuilder, ui);

			plugins.InitializeStoreAsync(pluginLoader).AsTask().Wait();
			plugins.InitalizePlugins(pluginInitializer);

			commandRepositoryBuilder.BakeToRepository(lservices.GetRequiredService<ICommandRepository>());
			bodyTypeResolverBuilder.BakeToResolver(lservices.GetRequiredService<IBodyTypeResolver>());

			client.MainLoop(plugins);
		}

		static AppBuilder BuildAvaloniaApp()
		{
			return AppBuilder.Configure<App>()
				.UsePlatformDetect()
				.LogToTrace();
		}
	}
}