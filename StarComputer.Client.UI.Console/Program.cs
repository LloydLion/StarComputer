using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using StarComputer.Client;
using StarComputer.Client.Abstractions;
using StarComputer.Common.Abstractions;
using StarComputer.Common.Abstractions.Plugins;
using StarComputer.Common.Abstractions.Plugins.Commands;
using StarComputer.Common.Abstractions.Plugins.ConsoleUI;
using StarComputer.Common.Abstractions.Protocol;
using StarComputer.Common.DebugEnv;
using StarComputer.Common.Plugins.Commands;
using StarComputer.Common.Abstractions.Utils;
using StarComputer.Common.Abstractions.Utils.Logging;
using StarComputer.UI.Console.Plugins;
using System.Net;
using StarComputer.Common.Threading;
using StarComputer.Common.Abstractions.Threading;

var services = new ServiceCollection()
	.Configure<ClientConfiguration>(config =>
	{

	})

	.AddSingleton<IClient, Client>()

	.AddTransient<IMessageHandler, PluginOrientedMessageHandler>()
	.AddTransient<IConsoleUIContext, ConsoleUIContext>()

	.AddSingleton<IThreadDispatcher<Action>>(new ThreadDispatcher<Action>(Thread.CurrentThread, s => s()))

	.AddSingleton<ICommandRepository, CommandRepository>()

	.AddSingleton<IPlugin>(new HelloPlugin.HelloPlugin())

	.AddLogging(builder => builder.SetMinimumLevel(LogLevel.Trace).AddFancyLogging())
	.BuildServiceProvider();


SynchronizationContext.SetSynchronizationContext(services.GetRequiredService<IThreadDispatcher<Action>>().CraeteSynchronizationContext(s => s));

var builder = new CommandRespositoryBuilder();

var initializer = new ClientPluginInitializer<IConsoleUIContext>(services.GetRequiredService<IClient>(), builder, services.GetRequiredService<IConsoleUIContext>());
initializer.InitializePlugins(services.GetServices<IPlugin>());

builder.BakeToRepository(services.GetRequiredService<ICommandRepository>());


Console.WriteLine("Server IP: 127.0.0.1");
IPAddress address = IPAddress.Parse("127.0.0.1");
Console.WriteLine("Server port: " + StaticInformation.ConnectionPort);
int port = StaticInformation.ConnectionPort;
Console.WriteLine("Server password: DEBUG PASSWORD");
var password = "DEBUG PASSWORD";
Console.Write("Login: ");
var login = Console.ReadLine() ?? throw new NullReferenceException();


services.GetRequiredService<IClient>().Connect(new ConnectionConfiguration(new(address, port), password, login));
