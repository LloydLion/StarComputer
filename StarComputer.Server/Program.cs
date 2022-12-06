using Microsoft.Extensions.DependencyInjection;
using StarComputer.Server;
using StarComputer.Server.DebugEnv;
using StarComputer.Shared.DebugEnv;
using StarComputer.Shared.Protocol;

var services = new ServiceCollection()
	.Configure<ServerConfiguration>(config =>
	{

	})

	.AddSingleton<IServer, Server>()

	.AddSingleton<IMessageHandler, HelloMessageHandler>()
	.AddSingleton<IClientApprovalAgent, GugApprovalAgent>()
	.AddLogging()
	.BuildServiceProvider();

services.GetRequiredService<IServer>().Listen();