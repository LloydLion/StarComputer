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
using System.Collections;
using System.Net.Mime;
using System.Net.NetworkInformation;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace ChatPlugin
{
	[Plugin]
	public class ChatPlugin : IPlugin
	{
		private IProtocolEnvironment? protocol = null;
		private IHTMLUIContext? ui = null;
		private readonly HTMLContext html;
		private readonly Dictionary<string, TaskCompletionSource<(FileMetadata?, byte[]?)>> fileWaits = new();


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

			resolverBuilder.RegisterAllias(typeof(FileRequest), "fileRequest");
			resolverBuilder.RegisterAllias(typeof(FileRequestResponce), "fileRequestResponce");
			resolverBuilder.RegisterAllias(typeof(UploadFileRequest), "uploadFileRequest");


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

					foreach (var el in fileWaits)
						el.Value.SetException(new Exception("Connection closed"));
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
				else if (message.Body is FileRequestResponce frr)
				{
					if (fileWaits.TryGetValue(frr.RequestedUUID, out var task))
					{
						byte[]? data = null;
						if (frr.AttachmentName is not null)
						{
							var attachment = message.Attachments?[frr.AttachmentName] ?? throw new NullReferenceException();
							var stream = new MemoryStream(attachment.Length);
							await attachment.CopyDelegate(stream);
						}

						task.SetResult((frr.File, data));
					}
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

		private Task<(FileMetadata?, byte[]?)> AwaitForFile(string fileUUID)
		{
			var task = new TaskCompletionSource<(FileMetadata?, byte[]?)>();
			fileWaits.Add(fileUUID, task);
			return task.Task;
		}

		private static async Task<(FileMetadata?, byte[]?)> GetFileServerSideAsync(string uuid)
		{
			var file = GetFileMetadataServerSide(uuid);
			if (file is null) return (null, null);
			using var stream = File.OpenRead(constructFileName(file));
			var buffer = new byte[stream.Length];
			await stream.ReadAsync(buffer);
			return (file, buffer);

			static string constructFileName(FileMetadata file)
			{
				var dirPath = Path.Combine("chat", "files");
				if (Directory.Exists(dirPath) == false)
					Directory.CreateDirectory(dirPath);

				return Path.Combine(dirPath, $"{file.UUID}.{file.Name}.{file.Extension}");
			}
		}

		private static FileMetadata? GetFileMetadataServerSide(string uuid)
		{
			var dirPath = Path.Combine("chat", "files");
			if (Directory.Exists(dirPath) == false) return null;

			var fileName = Directory.GetFiles(dirPath).FirstOrDefault(s => Path.GetFileName(s).StartsWith(uuid));
			if (fileName is null) return null;
			var splits = fileName.Split('.');
			return new(splits[1], splits[2]) { UUID = uuid };
		}

		private async Task UploadFileServerSideAsync(FileMetadata metadata, byte[] bytes)
		{
			var dirPath = Path.Combine("chat", "files");
			if (Directory.Exists(dirPath) == false)
				Directory.CreateDirectory(dirPath);

			var fileName = Path.Combine(dirPath, $"{metadata.UUID}.{metadata.Name}.{metadata.Extension}");
			await File.WriteAllBytesAsync(fileName, bytes);
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


			public record FileData(int[] Data, int RealLength);

			public async Task<FileData> GetFileBinaries(string uuid)
			{
				byte[] bytes;

				if (owner.Protocol is IClientProtocolEnviroment client)
				{
					var request = new FileRequest(uuid, requireBinaries: true);
					var message = new ProtocolMessage(owner.Domain, request, null, null);
					await client.Client.GetServerAgent().SendMessageAsync(message);

					var (_, rbytes) = await owner.AwaitForFile(uuid);
					bytes = rbytes ?? throw new Exception("File with given UUID doesn't exist");
				}
				else
				{
					var (_, rbytes) = await GetFileServerSideAsync(uuid);
					bytes = rbytes ?? throw new Exception("File with given UUID doesn't exist");
				}

				var ints = new int[bytes.Length % 4 == 0 ? bytes.Length / 4 : (bytes.Length / 4) + 1];
				Buffer.BlockCopy(bytes, 0, ints, 0, bytes.Length);
				return new FileData(ints, bytes.Length);
			}

			public async Task<FileMetadata> GetFileMetadata(string uuid)
			{
				if (owner.Protocol is IClientProtocolEnviroment client)
				{
					var request = new FileRequest(uuid, requireBinaries: false);
					var message = new ProtocolMessage(owner.Domain, request, null, null);
					await client.Client.GetServerAgent().SendMessageAsync(message);

					var (metadata, bytes) = await owner.AwaitForFile(uuid);
					return metadata ?? throw new Exception("File with given UUID doesn't exist");
				}
				else
				{
					return GetFileMetadataServerSide(uuid) ?? throw new Exception("File with given UUID doesn't exist");
				}
			}

			public async Task<string> UploadFile(string name, string extension, int[] intBytes, int realLength)
			{
				byte[] bytes = new byte[realLength];
				Buffer.BlockCopy(intBytes, 0, bytes, 0, realLength);

				if (owner.Protocol is IClientProtocolEnviroment client)
				{
					var messageFile = new FileMetadata(name, extension);
					var request = new UploadFileRequest(messageFile, "file");
					var message = new ProtocolMessage(owner.Domain, request, new[] { new ProtocolMessage.Attachment("file", async stream => await stream.WriteAsync(bytes), bytes.Length) }, null);

					await client.Client.GetServerAgent().SendMessageAsync(message);

					return messageFile.UUID;
				}
				else
				{
					var newMeta = new FileMetadata(name, extension);
					await owner.UploadFileServerSideAsync(newMeta, bytes);
					return newMeta.UUID;
				}
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

		private class UploadFileRequest
		{
			public UploadFileRequest(FileMetadata file, string attachmentName)
			{
				File = file;
				AttachmentName = attachmentName;
			}


			public FileMetadata File { get; }

			public string AttachmentName { get; }
		}

		private class FileRequestResponce
		{
			public FileRequestResponce(FileMetadata file, string? attachmentName = null)
			{
				File = file;
				RequestedUUID = file.UUID;
				AttachmentName = attachmentName;
			}

			public FileRequestResponce(string requestedUUID)
			{
				RequestedUUID = requestedUUID;
				AttachmentName = null;
			}


			public string RequestedUUID { get; }

			public FileMetadata? File { get; }

			public string? AttachmentName { get; }
		}

		private class FileRequest
		{
			public FileRequest(string uuid, bool requireBinaries)
			{
				UUID = uuid;
				RequireBinaries = requireBinaries;
			}


			public string UUID { get; }

			public bool RequireBinaries { get; }
		}

		private record FileMetadata(string Name, string Extension)
		{
			public string UUID { get; init; } = Guid.NewGuid().ToString();
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