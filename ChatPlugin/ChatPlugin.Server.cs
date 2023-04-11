using Microsoft.Extensions.DependencyInjection;
using StarComputer.Common.Abstractions.Plugins;
using StarComputer.Common.Abstractions.Plugins.Persistence;
using StarComputer.Common.Abstractions.Plugins.Protocol;
using StarComputer.Common.Abstractions.Plugins.UI.HTML;
using StarComputer.PluginDevelopmentKit;
using StarComputer.Server.Abstractions;
using System.Diagnostics.CodeAnalysis;

namespace ChatPlugin
{
	public partial class ChatPlugin : PluginBase
	{
		private const string MessagesPersistenceAddress = "messages/";
		private const string BroadcastMessagesFileName = ".broadcast";
		private const string MessagesFileExtension = ".data";
		private static readonly PersistenceAddress BroadcastMessagesPersistenceAddress = new(MessagesPersistenceAddress + BroadcastMessagesFileName + MessagesFileExtension);


		private readonly ServerUIContext serverUI;


		protected override void Initialize(IServerProtocolEnvironment serverProtocolEnvironment)
		{
			var broadcast = persistence.ReadObject<MessageCollection>(BroadcastMessagesPersistenceAddress);

			ui.LoadHTMLPage(new("server.html"), new PageConstructionBag().AddConstructionArgument("InitialMessages", broadcast.Select(s => new MessageUIDTO(s)), useJson: true));
			ui.SetJSPluginContext(serverUI);
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

		private IEnumerable<Message> FormMessagesForClient(PluginUser client)
		{
			var store = client.GetRequiredService<UserMessageStore>();

			var broadcast = persistence.ReadObject<MessageCollection>(BroadcastMessagesPersistenceAddress);
			return store.ListMessages().Concat(broadcast);
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
		}


		private class MessageCollection : List<Message> { }

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
			private readonly ChatPlugin owner;


			public ServerUIContext(ChatPlugin owner)
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

				owner.VisualizeServerMessage(message.Message, reciverUserName);

				await Task.WhenAll(targets.Select(target => target.Agent.SendMessageAsync(new(message, null))));
			}
		}
	}
}
