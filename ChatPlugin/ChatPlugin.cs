using Newtonsoft.Json;
using StarComputer.Client.Abstractions;
using StarComputer.Common.Abstractions.Plugins;
using StarComputer.Common.Abstractions.Plugins.Commands;
using StarComputer.Common.Abstractions.Plugins.Loading;
using StarComputer.Common.Abstractions.Plugins.UI.HTML;
using StarComputer.Common.Abstractions.Protocol;
using StarComputer.Common.Abstractions.Protocol.Bodies;
using StarComputer.Server.Abstractions;
using System.Buffers;
using System.Net.Mime;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace ChatPlugin
{
	[Plugin]
	public class ChatPlugin : IPlugin
	{
		private IProtocolEnvironment? protocol = null;
		private IHTMLUIContext? ui = null;
		private readonly HTMLContext html;


		public ChatPlugin()
		{
			html = new(this);
		}


		public string UserName => Protocol is IClientProtocolEnviroment client ? "/" + client.Client.GetConnectionConfiguration().Login : "Server";

		public string Domain => "Chat";

		public IReadOnlyCollection<Type> TargetUIContextTypes { get; } = new[] { typeof(IHTMLUIContext) };

		public Version Version { get; } = Assembly.GetExecutingAssembly().GetName().Version!;

		private IProtocolEnvironment Protocol => protocol!;

		private IHTMLUIContext UI => ui!;


		public void InitializeAndBuild(IProtocolEnvironment protocolEnviroment, IUIContext uiContext, ICommandRepositoryBuilder commandsBuilder, IBodyTypeResolverBuilder resolverBuilder)
		{
			resolverBuilder.RegisterAllias(typeof(ChatRequest), "chatRequest");
			resolverBuilder.RegisterAllias(typeof(ChatResponce), "chatResponce");
			resolverBuilder.RegisterAllias(typeof(NewMessage), "newMessage");

			ui = (IHTMLUIContext)uiContext;
			protocol = protocolEnviroment;

			UI.LoadEmptyPage();

			if (protocol is IClientProtocolEnviroment client)
			{
				client.ClientConnected += async () =>
				{
					try
					{
						await client.Client.GetServerAgent().SendMessageAsync(new(Domain, new ChatRequest(), null, null));
					}
					catch (Exception) { }
				};

				client.ClientDisconnected += () =>
				{
					UI.LoadEmptyPage();
				};
			}
			else
			{
				using var brFile = new StreamReader(OpenMessagesFile((string?)null));
				var brMessages = JsonConvert.DeserializeObject<Message[]>(brFile.ReadToEnd()) ?? Array.Empty<Message>();

				UI.LoadHTMLPage("server.html", new PageConstructionBag().AddConstructionArgument("InitialMessages", brMessages.Select(s => new MessageUIDTO(s)), useJson: true));
				UI.SetJSPluginContext(html);
			}
		}

		public async ValueTask ProcessMessageAsync(ProtocolMessage message, IMessageContext messageContext)
		{
			if (Protocol is IServerProtocolEnvironment server)
			{
				if (message.Body is ChatRequest)
				{
					using var file = new StreamReader(OpenMessagesFile(messageContext.Agent));
					IEnumerable<Message> prMessages = JsonConvert.DeserializeObject<Message[]>(file.ReadToEnd()) ?? Array.Empty<Message>();

					using var brFile = new StreamReader(OpenMessagesFile((string?)null));
					IEnumerable<Message> brMessages = JsonConvert.DeserializeObject<Message[]>(brFile.ReadToEnd()) ?? Array.Empty<Message>();

					var messages = prMessages.Concat(brMessages).OrderBy(s => s.TimeSpamp);

					try
					{
						await messageContext.Agent.SendMessageAsync(new(Domain, new ChatResponce() { Messages = messages }, null, null));
					}
					catch (Exception) { }


					foreach (var msg in messages)
						UI.ExecuteJavaScriptFunction("visualizeMessageSS", GetAgentUserName(messageContext.Agent), new MessageUIDTO(msg));
				}
				else if (message.Body is NewMessage msg)
				{
					AppendMessagesFile(messageContext.Agent, msg.Message);

					UI.ExecuteJavaScriptFunction("visualizeMessageSS", GetAgentUserName(messageContext.Agent), new MessageUIDTO(msg.Message));
				}
			}
			else if (Protocol is IClientProtocolEnviroment client)
			{
				if (message.Body is ChatResponce cr)
				{
					UI.LoadHTMLPage("client.html", new PageConstructionBag().AddConstructionArgument("InitialMessages", cr.Messages.Select(s => new MessageUIDTO(s)), useJson: true));
					UI.SetJSPluginContext(html);
				}
				else if (message.Body is NewMessage msg)
				{
					UI.ExecuteJavaScriptFunction("visualizeMessageCS", new MessageUIDTO(msg.Message));
				}
			}
		}

		public ValueTask ProcessCommandAsync(PluginCommandContext commandContext)
		{
			return ValueTask.CompletedTask;
		}

		private string GetAgentUserName(IRemoteProtocolAgent agent)
		{
			if (Protocol is IServerProtocolEnvironment server)
			{
				return "/" + server.Server.GetClientByAgent(agent).ConnectionInformation.Login;
			}
			else return "Server";
		}

		private void AppendMessagesFile(IRemoteProtocolAgent agent, Message msg)
		{
			if (Protocol is IServerProtocolEnvironment server)
			{
				AppendMessagesFile(server.Server.GetClientByAgent(agent).ConnectionInformation.Login, msg);
			}
			else throw new Exception("Server side method only");
		}

		private static void AppendMessagesFile(string? userName, Message msg)
		{
			using var file = OpenMessagesFile(userName);
			var writer = new StreamWriter(file);
			var reader = new StreamReader(file);

			var messages = JsonConvert.DeserializeObject<List<Message>>(reader.ReadToEnd()) ?? new List<Message>();
			messages.Add(msg);

			if (messages.Count > 100)
				messages.RemoveRange(0, messages.Count - 100);

			file.Position = 0;

			writer.Write(JsonConvert.SerializeObject(messages, Formatting.None));
			writer.Flush();
		}

		private static FileStream OpenMessagesFile(string? userName)
		{
			var dirPath = Path.Combine("chat", "messages");
			if (Directory.Exists(dirPath) == false)
				Directory.CreateDirectory(dirPath);

			var filePath = Path.Combine(dirPath, userName ?? "broadcast");
			return File.Open(filePath, FileMode.OpenOrCreate);
		}

		private FileStream OpenMessagesFile(IRemoteProtocolAgent agent)
		{
			if (Protocol is IServerProtocolEnvironment server)
			{
				return OpenMessagesFile(server.Server.GetClientByAgent(agent).ConnectionInformation.Login);
			}
			else throw new Exception("Server side method only");
		}


		private class HTMLContext
		{
			private readonly ChatPlugin owner;


			public HTMLContext(ChatPlugin owner)
			{
				this.owner = owner;
			}


			public async Task SendMessage(string? reciver, string content, int contentType)
			{
				var message = new NewMessage(new(owner.UserName, content, (Message.ContentType)contentType, DateTime.Now));

				IEnumerable<IRemoteProtocolAgent> targets;

				if (owner.Protocol is IServerProtocolEnvironment server)
				{
					var mtargets = server.Server.ListClients();
					if (reciver is not null) mtargets = mtargets.Where(s => s.ConnectionInformation.Login == reciver);
					targets = mtargets.Select(s => s.ProtocolAgent);

					AppendMessagesFile(reciver, message.Message);
				}
				else if (owner.Protocol is IClientProtocolEnviroment client)
				{
					targets = new[] { client.Client.GetServerAgent() };
				}
				else throw new Exception();

				try
				{
					await Task.WhenAll(targets.Select(target => target.SendMessageAsync(new(owner.Domain, message, null, null))));
				}
				catch (Exception) { }
			}
		}

		private class ChatRequest { }

		private class NewMessage
		{
			public Message Message { get; set; }


			public NewMessage(Message message)
			{
				Message = message;
			}
		}

		private class ChatResponce
		{
			public IEnumerable<Message> Messages { get; set; } = Array.Empty<Message>();
		}

		private record Message(string Author, string Content, Message.ContentType Type, DateTime TimeSpamp)
		{
			public enum ContentType
			{
				Text,
				Url,
				File
			}
		}

		private class MessageUIDTO
		{
			public MessageUIDTO(Message message)
			{
				Author = message.Author;
				Content = message.Content;
				Type = (int)message.Type;
			}


			public string Author { get; }

			public string Content { get; }

			public int Type { get; }
		}
	}
}