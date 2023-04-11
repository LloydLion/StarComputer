using Microsoft.Extensions.DependencyInjection;
using StarComputer.Client.Abstractions;
using StarComputer.Common.Abstractions.Plugins.Protocol;
using StarComputer.Common.Abstractions.Plugins;
using StarComputer.Common.Abstractions.Plugins.Resources;
using StarComputer.Common.Abstractions.Plugins.UI.HTML;
using StarComputer.PluginDevelopmentKit;
using StarComputer.Server.Abstractions;
using System.Diagnostics.CodeAnalysis;

namespace ChatPlugin
{
	public partial class ChatPlugin : PluginBase
	{
		private readonly ClientUIContext clientUI;


		protected override void Initialize(IClientProtocolEnviroment clientProtocolEnviroment)
		{
			clientProtocolEnviroment.Client.ClientConnected += async () =>
			{
				var responce = await SendMessageAndRequestResponse<ClientChatInitializationModel>(clientProtocolEnviroment.Client.GetServerAgent(), new ClientChatInitializationRequest());
				var messages = responce.Body.Messages;

				ui.LoadHTMLPage(new PluginResource("client.html"), new PageConstructionBag().AddConstructionArgument("InitialMessages", messages.Select(s => new MessageUIDTO(s)), useJson: true));
				ui.SetJSPluginContext(clientUI);
			};

			clientProtocolEnviroment.Client.ClientDisconnected += () =>
			{
				ui.LoadEmptyPage();
			};
		}


		[MessageProcessor]
		[SuppressMessage("Style", "IDE0060"), SuppressMessage("CodeQuality", "IDE0051")]
		private void ProcessServerMessage(IClientProtocolEnviroment environment, PluginProtocolMessage message, MessageContext messageContext, MessageSendPackage messagePackage)
		{
			VisualizeClientMessage(messagePackage.Message);
		}

		private void VisualizeClientMessage(Message message)
		{
			ClientOnly();
			ui.ExecuteJavaScriptFunction("visualizeMessageCS", new MessageUIDTO(message));
		}


		private class ClientUIContext : CommonUIContext
		{
			private readonly ChatPlugin owner;


			public ClientUIContext(ChatPlugin owner)
			{
				this.owner = owner;
			}


			public IClientProtocolEnviroment ClientEnvironment => owner.ClientOnly();


			public async Task SendMessage(string content, string contentType)
			{
				var message = new MessageSendPackage(new(owner.CurrentUserName, content, Enum.Parse<Message.ContentType>(contentType), DateTime.Now));

				owner.VisualizeClientMessage(message.Message);

				await ClientEnvironment.Client.GetServerAgent().SendMessageAsync(new(message));
			}
		}
	}
}
