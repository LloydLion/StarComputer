using Microsoft.Extensions.DependencyInjection;
using StarComputer.Server;

var services = new ServiceCollection()
	.Configure<ServerConfiguration>(config =>
	{

	})

	.AddSingleton<IServer, Server>()
	.BuildServiceProvider();

services.GetRequiredService<IServer>().Listen();