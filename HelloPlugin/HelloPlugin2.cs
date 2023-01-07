using StarComputer.Client.Abstractions;
using StarComputer.Common.Abstractions.Plugins;
using StarComputer.Common.Abstractions.Plugins.Commands;
using StarComputer.Common.Abstractions.Plugins.UI.Console;
using StarComputer.Common.Abstractions.Plugins.Loading;
using StarComputer.Common.Abstractions.Protocol;
using StarComputer.Common.Abstractions.Protocol.Bodies;
using StarComputer.Server.Abstractions;
using System.Reflection;
using StarComputer.Common.Abstractions.Plugins.UI.HTML;

namespace HelloPlugin
{
	[Plugin]
	public class HelloPlugin2 : IPlugin
	{
		private IProtocolEnvironment? protocol = null;
		private IUIContext? ui = null;
		private readonly MagicContext htmlUIContext;


		private IProtocolEnvironment Protocol => protocol!;

		private IUIContext UI => ui!;

		private string UserName => Protocol is IClientProtocolEnviroment client ? "Client/" + client.Client.GetConnectionConfiguration().Login : "Server";

		public string Domain => "Hello2";

		public IReadOnlyCollection<Type> TargetUIContextTypes { get; } = new[] { typeof(IConsoleUIContext), typeof(IHTMLUIContext) };

		public Version Version { get; } = Assembly.GetExecutingAssembly().GetName().Version!;


		public HelloPlugin2()
		{
			htmlUIContext = new(SendMessage);
		}


		public void InitializeAndBuild(
			IProtocolEnvironment protocolEnviroment,
			IUIContext uiContext,
			ICommandRepositoryBuilder commandsBuilder,
			IBodyTypeResolverBuilder resolverBuilder)
		{
			resolverBuilder.RegisterAllias(typeof(GreetingBody), "greeting");

			ui = uiContext;
			protocol = protocolEnviroment;

			if (uiContext is IConsoleUIContext consoleUI)
			{
				consoleUI.NewLineSent += async (line) => await SendMessage(line);
			}
			else if (uiContext is IHTMLUIContext htmlUI)
			{
				htmlUI.LoadHTMLPage("demo2.html", new());
				htmlUI.SetJSPluginContext(htmlUIContext);
			}
			else throw new NotSupportedException("HelloPlugin doesn't support UI context of type " + uiContext.GetType().FullName);
		}

		public ValueTask ProcessCommandAsync(PluginCommandContext commandContext)
		{
			return ValueTask.CompletedTask;
		}

		public async ValueTask ProcessMessageAsync(ProtocolMessage message, IMessageContext messageContext)
		{
			if (message.Body is GreetingBody body)
			{
				if (Protocol is IServerProtocolEnvironment server)
				{
					var clients = server.Server.ListClients().Where(s => s.ProtocolAgent != messageContext.Agent);

					foreach (var client in clients)
						await client.ProtocolAgent.SendMessageAsync(message);
				}


				if (UI is IConsoleUIContext consoleUI)
				{
					if (body.TargetUser is null)
						consoleUI.Out.WriteLine($"[{body.OriginUser}]: " + body.Message);
					else if (body.TargetUser == UserName)
						consoleUI.Out.WriteLine($"!DM! [{body.OriginUser}]: " + body.Message);

				}
				else if (UI is IHTMLUIContext htmlUI)
				{
					if (body.TargetUser is null)
						htmlUI.ExecuteJavaScriptFunction("showMessage", $"\"[{body.OriginUser}]: {body.Message}\"");
					else if (body.TargetUser == UserName)
						htmlUI.ExecuteJavaScriptFunction("showMessage", $"\"!DM! [{body.OriginUser}]: {body.Message}\"");
				}
				else throw new NotSupportedException("HelloPlugin doesn't support UI context of type " + UI.GetType().FullName);
			}
		}

		private async Task SendMessage(string line)
		{
			if (line.StartsWith("!") == false) return;
			line = line[1..];

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

			if (Protocol is IServerProtocolEnvironment server)
			{
				foreach (var client in server.Server.ListClients())
					await client.ProtocolAgent.SendMessageAsync(message);
			}
			else if (Protocol is IClientProtocolEnviroment client)
			{
				await client.Client.GetServerAgent().SendMessageAsync(message);
			}
		}


		private class GreetingBody
		{
			public string Message { get; set; } = "";

			public string OriginUser { get; set; } = "";

			public string? TargetUser { get; set; } = null;
		}

		private class MagicContext
		{
			private readonly Func<string, Task> sendMessageDelegate;


			public MagicContext(Func<string, Task> sendMessageDelegate)
			{
				this.sendMessageDelegate = sendMessageDelegate;
			}


			public Task SendMessage(string line) => sendMessageDelegate(line);
		}
	}
}
