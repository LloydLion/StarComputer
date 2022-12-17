using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using StarComputer.Client;
using StarComputer.Client.Abstractions;
using StarComputer.Common;
using StarComputer.Common.Abstractions;
using StarComputer.Common.Abstractions.Protocol;
using StarComputer.Common.DebugEnv;
using StarComputer.Common.Protocol;
using StarComputer.Common.Utils.Logging;
using System.Net;

Thread.Sleep(2500);

var services = new ServiceCollection()
	.Configure<ClientConfiguration>(config =>
	{

	})

	.AddSingleton<IClient, Client>()
	.AddSingleton<IMessageHandler, HelloMessageHandler>()
	.AddLogging(builder => builder.SetMinimumLevel(LogLevel.Trace).AddFancyLogging())
	.BuildServiceProvider();

Console.WriteLine("Server IP: 127.0.0.1");
IPAddress address = IPAddress.Parse("127.0.0.1");//IPAddress.Parse(Console.ReadLine() ?? throw new NullReferenceException());
Console.WriteLine("Server port: " + StaticInformation.ConnectionPort);
int port = StaticInformation.ConnectionPort;//int.Parse(Console.ReadLine() ?? throw new NullReferenceException());
Console.WriteLine("Server password: DEBUG PASSWORD");
var password = "DEBUG PASSWORD";//Console.ReadLine() ?? throw new NullReferenceException();
Console.Write("Login: ");
var login = Console.ReadLine() ?? throw new NullReferenceException();

services.GetRequiredService<IClient>().Connect(new(address, port), password, login);
