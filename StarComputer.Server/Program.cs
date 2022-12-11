using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using StarComputer.Server;
using StarComputer.Server.DebugEnv;
using StarComputer.Shared.DebugEnv;
using StarComputer.Shared.Protocol;
using StarComputer.Shared.Utils.Logging;

var services = new ServiceCollection()
	.Configure<ServerConfiguration>(config =>
	{

	})

	.AddSingleton<IServer, Server>()

	.AddSingleton<IMessageHandler, HelloMessageHandler>()
	.AddSingleton<IClientApprovalAgent, GugApprovalAgent>()
	.AddLogging(builder => builder.SetMinimumLevel(LogLevel.Trace).AddFancyLogging())
	.BuildServiceProvider();

services.GetRequiredService<IServer>().Listen();