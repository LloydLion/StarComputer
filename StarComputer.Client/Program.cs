using Microsoft.Extensions.DependencyInjection;
using StarComputer.Client;
using StarComputer.Shared;
using StarComputer.Shared.DebugEnv;
using StarComputer.Shared.Protocol;
using System.Net;

Thread.Sleep(2500);

var services = new ServiceCollection()
	.Configure<ClientConfiguration>(config =>
	{

	})

	.AddSingleton<IClient, Client>()

	.AddSingleton<IMessageHandler, HelloMessageHandler>()
	.BuildServiceProvider();

Console.Write("Server ip: 127.0.0.1");
IPAddress address = IPAddress.Parse("127.0.0.1");//IPAddress.Parse(Console.ReadLine() ?? throw new NullReferenceException());
Console.Write("Server port: " + StaticInformation.ConnectionPort);
int port = StaticInformation.ConnectionPort;//int.Parse(Console.ReadLine() ?? throw new NullReferenceException());
Console.Write("Server password: DEBUG PASSWORD");
var password = "DEBUG PASSWORD";//Console.ReadLine() ?? throw new NullReferenceException();
Console.Write("Login: ");
var login = Console.ReadLine() ?? throw new NullReferenceException();

services.GetRequiredService<IClient>().Connect(new(address, port), password, login);
