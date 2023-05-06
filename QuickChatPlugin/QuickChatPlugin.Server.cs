using Microsoft.Extensions.DependencyInjection;
using StarComputer.Common.Abstractions.Plugins;
using StarComputer.Common.Abstractions.Plugins.Persistence;
using StarComputer.Common.Abstractions.Plugins.Protocol;
using StarComputer.Common.Abstractions.Plugins.Resources;
using StarComputer.Common.Abstractions.Plugins.UI.HTML;
using StarComputer.PluginDevelopmentKit;
using StarComputer.Server.Abstractions;
using System;
using System.Diagnostics.CodeAnalysis;

namespace QuickChatPlugin
{
	public partial class QuickChatPlugin : PluginBase
	{
		private const string MessagesPersistenceAddress = "messages/";
		private const string FilesPersistenceAddress = "files/";
		private const string FilesMetadataFileName = ".filesmeta";
		private const string BroadcastMessagesFileName = ".broadcast";
		private const string MessagesFileExtension = ".data";
		private static readonly PersistenceAddress BroadcastMessagesPersistenceAddress = new(MessagesPersistenceAddress + BroadcastMessagesFileName + MessagesFileExtension);


		private readonly ServerUIContext serverUI;


		protected override async void Initialize(IServerProtocolEnvironment serverProtocolEnvironment)
		{
			var broadcast = persistence.ReadObject<MessageCollection>(BroadcastMessagesPersistenceAddress);

			await ui.LoadHTMLPageAsync(new("server.html"), new PageConstructionBag(localizer).AddConstructionArgument("InitialMessages", broadcast.Select(s => new MessageUIDTO(s)), useJson: true));
			ui.SetJSPluginContext(serverUI);
			ui.ExecuteJavaScriptFunction("initialize");
		}


		[MessageProcessor]
		[SuppressMessage("Style", "IDE0060"), SuppressMessage("CodeQuality", "IDE0051")]
		private async ValueTask ProcessClientInitialization(IServerProtocolEnvironment environment, PluginProtocolMessage message, MessageContext messageContext, ClientChatInitializationRequest request)
		{
			var sender = messageContext.Agent;

			var client = AssociateUser(sender);
			InitializeUser(client);

			var messages = FormMessagesForClient(client);

			await sender.SendMessageAsync(new(new ClientChatInitializationModel(messages)));
		}

		[MessageProcessor]
		[SuppressMessage("Style", "IDE0060"), SuppressMessage("CodeQuality", "IDE0051")]
		private void ProcessClientMessage(IServerProtocolEnvironment environment, PluginProtocolMessage message, MessageContext messageContext, MessageSendPackage messagePackage)
		{
			var sender = messageContext.Agent;

			var client = AssociateUser(sender);

			client.GetRequiredService<UserMessageStore>().AddMessage(messagePackage.Message);
		}

		[MessageProcessor]
		[SuppressMessage("Style", "IDE0060"), SuppressMessage("CodeQuality", "IDE0051")]
		private async ValueTask ProcessClientFileUpload(IServerProtocolEnvironment environment, PluginProtocolMessage message, MessageContext messageContext, UploadFileRequest uploadRequest)
		{
			var file = message.Attachment ?? throw new NullReferenceException();
			using var memory = new MemoryStream(file.Length);
			await file.CopyDelegate(memory);
			var buffer = memory.GetBuffer();
			var uuid = await SaveFileAsync(uploadRequest.FullFileName, buffer);

			await messageContext.Agent.SendMessageAsync(new(new UploadFileResponce(uuid.ToString())));
		}

		[MessageProcessor]
		[SuppressMessage("Style", "IDE0060"), SuppressMessage("CodeQuality", "IDE0051")]
		private async ValueTask ProcessClientFileLoad(IServerProtocolEnvironment environment, PluginProtocolMessage message, MessageContext messageContext, LoadFileRequest loadRequest)
		{
			if (Guid.TryParse(loadRequest.UUID, out var guid))
			{		
				using var files = persistence.GetObject<FileMetaCollection>(new PersistenceAddress(FilesPersistenceAddress + FilesMetadataFileName));

				var fileMeta = files.Object[guid];
				fileMeta.Use();

				var responce = new LoadFileResponce(fileMeta.FileName, fileMeta.Extension);

				if (loadRequest.NeedAddFileContent == false)
				{
					await messageContext.Agent.SendMessageAsync(new(responce));
				}
				else
				{
					var rawData = await persistence.LoadRawDataAsync(new PersistenceAddress(FilesPersistenceAddress + guid.ToString()));
					var attachment = new PluginProtocolMessage.MessageAttachment("fileContent", (stream) => stream.WriteAsync(rawData), rawData.Length);
					await messageContext.Agent.SendMessageAsync(new(responce, attachment));
				}
			}
			else throw new ArgumentException("No file with UUID " + loadRequest.UUID);
		}

		private IEnumerable<Message> FormMessagesForClient(PluginUser client)
		{
			var store = client.GetRequiredService<UserMessageStore>();

			var broadcast = persistence.ReadObject<MessageCollection>(BroadcastMessagesPersistenceAddress);
			return store.ListMessages().Concat(broadcast).OrderBy(s => s.TimeSpamp);
		}

		private void InitializeUser(PluginUser client)
		{
			var userName = client.UserName;

			var store = new UserMessageStore(client, persistence);
			store.SetAddCallback(s =>
			{
				VisualizeServerMessage(s, userName);
			});

			store.ReadMessages();
			client.RegisterService(store, typeof(UserMessageStore));
		}

		private void VisualizeServerMessage(Message message, string? visualizationPage)
		{
			ServerOnly();
			ui.ExecuteJavaScriptFunction("visualizeMessageSS", visualizationPage, new MessageUIDTO(message));
		}

		private void DispatchNewBroadcastMessage(Message message)
		{
			using var broadcast = persistence.GetObject<MessageCollection>(BroadcastMessagesPersistenceAddress);
			broadcast.Object.Add(message);

			VisualizeServerMessage(message, null);
		}

		private async Task<Guid> SaveFileAsync(string fullFileName, ReadOnlyMemory<byte> data)
		{
			var uuid = Guid.NewGuid();
			using var files = persistence.GetObject<FileMetaCollection>(new PersistenceAddress(FilesPersistenceAddress + FilesMetadataFileName));

			var fileName = Path.GetFileNameWithoutExtension(fullFileName);
			var extension = Path.GetExtension(fullFileName);
			files.Object.Add(uuid, new(fileName, extension));

			await persistence.SaveRawDataAsync(new PersistenceAddress(FilesPersistenceAddress + uuid.ToString()), data);

			return uuid;
		}


		private class MessageCollection : List<Message> { }

		private class FileMetaCollection : Dictionary<Guid, ServerSideFileMeta> { }

		private record ServerSideFileMeta(string FileName, string Extension)
		{
			private const int OutdateTimeoutInDays = 5;


			public DateTime LastUsage { get; private set; } = DateTime.Now;


			public bool IsOutdated() => (DateTime.Now - LastUsage).TotalDays > OutdateTimeoutInDays;

			public void Use() => LastUsage = DateTime.Now;
		}

		private class UserMessageStore
		{
			private readonly IPluginPersistenceService persistence;
			private readonly PersistenceAddress address;
			private Action<Message>? callback;


			public UserMessageStore(PluginUser user, IPluginPersistenceService persistence)
			{
				this.persistence = persistence;
				address = new PersistenceAddress(MessagesPersistenceAddress + user.ClientLogin + MessagesFileExtension);
			}


			public void AddMessage(Message message)
			{
				using (var array = persistence.GetObject<MessageCollection>(address))
				{
					array.Object.Add(message);
				};

				callback?.Invoke(message);
			}

			public void AddMessages(IEnumerable<Message> messageEnumberable)
			{
				using (var array = persistence.GetObject<MessageCollection>(address))
				{
					array.Object.AddRange(messageEnumberable);
				};

				foreach (var item in messageEnumberable)
					callback?.Invoke(item);
			}

			public IReadOnlyList<Message> ListMessages() => persistence.ReadObject<MessageCollection>(address);

			public void ReadMessages()
			{
				foreach (var item in ListMessages())
					callback?.Invoke(item);
			}

			public void SetAddCallback(Action<Message> callback) => this.callback = callback;
		}

		private class ServerUIContext : CommonUIContext
		{
			private readonly QuickChatPlugin owner;


			public ServerUIContext(QuickChatPlugin owner)
			{
				this.owner = owner;
			}


			public IServerProtocolEnvironment ServerEnvironment => owner.ServerOnly();


			public async Task SendMessage(string? reciverUserName, string content, string contentType)
			{
				var message = new MessageSendPackage(new(owner.CurrentUserName, content, Enum.Parse<Message.ContentType>(contentType), DateTime.Now));

				var targets = ServerEnvironment.Server.ListClients();

				if (reciverUserName is not null)
				{
					var reciverLogin = reciverUserName[ClientUserNamePrefix.Length..];

					targets = targets.Where(s => s.ConnectionInformation.Login == reciverLogin);
					var singleTarget = targets.Single();

					var user = owner.AssociateUser(singleTarget.Agent);
					var messages = user.GetRequiredService<UserMessageStore>();
					messages.AddMessage(message.Message);
				}
				else
				{
					//targets are all clients
					owner.DispatchNewBroadcastMessage(message.Message);
				}

				await Task.WhenAll(targets.Select(target => target.Agent.SendMessageAsync(new(message, null))));
			}

			public async Task<string> UploadFile(string fileName, int[] data, int realDataLength)
			{
				var bytesData = new byte[realDataLength];
				Buffer.BlockCopy(data, 0, bytesData, 0, realDataLength);

				return (await owner.SaveFileAsync(fileName, bytesData)).ToString();
			}

			public Task<FileMetaUIDTO> GetFileMeta(string uuid)
			{
				if (Guid.TryParse(uuid, out var guid))
				{
					using var files = owner.persistence.GetObject<FileMetaCollection>(new PersistenceAddress(FilesPersistenceAddress + FilesMetadataFileName));

					if (files.Object.TryGetValue(guid, out var value))
					{
						value.Use();
						return Task.FromResult(new FileMetaUIDTO(value.FileName, value.Extension));
					}
				}

				throw new ArgumentException("No file with UUID " + uuid);
			}

			public async Task<string> LoadFile(string uuid)
			{
				var rawData = await owner.persistence.LoadRawDataAsync(new PersistenceAddress(FilesPersistenceAddress + uuid));

				var resource = new PluginResource("file");

				owner.ui.StopResourceShare(resource);
				var address = owner.ui.ShareResource(resource, rawData, "application/octet-stream");

				return address;
			}
		}
	}
}
