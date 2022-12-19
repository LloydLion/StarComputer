using StarComputer.Client.Abstractions;
using StarComputer.Common.Abstractions.Plugins;
using StarComputer.Common.Abstractions.Plugins.Commands;
using StarComputer.Common.Abstractions.Plugins.ConsoleUI;
using StarComputer.Common.Abstractions.Protocol;
using StarComputer.Server.Abstractions;

namespace HelloPlugin
{
	public class HelloPlugin : IPlugin
	{
		private IProtocolEnvironment? protocol = null;
		private IConsoleUIContext? ui = null;


		private IProtocolEnvironment Protocol => protocol ?? throw new NullReferenceException();

		private IConsoleUIContext UI => ui ?? throw new NullReferenceException();

		private string UserName => Protocol is IClientProtocolEnviroment client ? "Client/" + client.Client.GetConnectionConfiguration().Login : "Server";

		public string Domain => "Hello";

		public Type TargetUIContextType => typeof(IConsoleUIContext);


		public void Initialize(IProtocolEnvironment protocolEnviroment, IUIContext uiContext)
		{
			ui = (IConsoleUIContext)uiContext;
			protocol = protocolEnviroment;

			ui.NewLineSent += onLineSent;

			async void onLineSent(string line)
			{
				var body = new GreetingBody() { OriginUser = UserName };

				if (line.Contains('\t'))
				{
					var splits = line.Split('\t', 2);

					body.TargetUser = splits[0];
					body.Message = splits[1];
				}
				else
				{
					body.TargetUser = null;
					body.Message = line;
				}


				var message = new ProtocolMessage(Domain, body, null, null);

				if (protocolEnviroment is IServerProtocolEnvironment server)
				{
					foreach (var client in server.Server.ListClients())
						await client.ProtocolAgent.SendMessageAsync(message);
				}
				else if (protocolEnviroment is IClientProtocolEnviroment client)
				{
					await client.Client.GetServerAgent().SendMessageAsync(message);
				}
			}
		}

		public void LoadCommands(ICommandRepositoryBuilder repository)
		{
			
		}

		public ValueTask ProcessCommandAsync(PluginCommandContext commandContext)
		{
			return ValueTask.CompletedTask;
		}

		public async ValueTask ProcessMessageAsync(ProtocolMessage message, IMessageContext messageContext)
		{
			if (message.Body is GreetingBody body)
			{
				if (body.TargetUser is null)
				{
					UI.Out.WriteLine($"[{body.OriginUser}]: " + body.Message);
				}
				else if (body.TargetUser == UserName)
				{
					UI.Out.WriteLine($"!DM! [{body.OriginUser}]: " + body.Message);
				}
				

				if (Protocol is IServerProtocolEnvironment server)
				{
					var clients = server.Server.ListClients().Where(s => s.ProtocolAgent != messageContext.Agent);

					foreach (var client in clients)
						await client.ProtocolAgent.SendMessageAsync(message);
				}
			}
		}


		private class GreetingBody
		{
			public string Message { get; set; } = "";

			public string OriginUser { get; set; } = "";

			public string? TargetUser { get; set; } = null;
		}
	}
}
