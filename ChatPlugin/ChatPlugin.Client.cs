using Microsoft.Extensions.DependencyInjection;
using StarComputer.Client.Abstractions;
using StarComputer.Common.Abstractions.Plugins.Protocol;
using StarComputer.Common.Abstractions.Plugins;
using StarComputer.Common.Abstractions.Plugins.Resources;
using StarComputer.Common.Abstractions.Plugins.UI.HTML;
using StarComputer.PluginDevelopmentKit;
using StarComputer.Server.Abstractions;
using System.Diagnostics.CodeAnalysis;
using StarComputer.Common.Abstractions.Plugins.Persistence;

namespace ChatPlugin
{
	public partial class ChatPlugin : PluginBase
	{
		private readonly ClientUIContext clientUI;


		protected override void Initialize(IClientProtocolEnviroment clientProtocolEnviroment)
		{
			clientProtocolEnviroment.Client.ClientConnected += async (sender, e) =>
			{
				var responce = await SendMessageAndRequestResponse<ClientChatInitializationModel>(clientProtocolEnviroment.Client.GetServerAgent(), new ClientChatInitializationRequest());
				var messages = responce.Body.Messages;

				await ui.LoadHTMLPageAsync(new PluginResource("client.html"), new PageConstructionBag().AddConstructionArgument("InitialMessages", messages.Select(s => new MessageUIDTO(s)), useJson: true));
				ui.SetJSPluginContext(clientUI);
				ui.ExecuteJavaScriptFunction("initialize");
			};

			clientProtocolEnviroment.Client.ClientDisconnected += async (sender, e) =>
			{
				await ui.LoadEmptyPageAsync();
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

			public async Task<string> UploadFile(string fileName, int[] data, int realDataLength)
			{
				var bytesData = new byte[realDataLength];
				Buffer.BlockCopy(data, 0, bytesData, 0, realDataLength);

				var message = new PluginProtocolMessage(new UploadFileRequest(fileName), new PluginProtocolMessage.MessageAttachment("file", (stream) => stream.WriteAsync(bytesData), realDataLength));
				var responce = await owner.SendMessageAndRequestResponse<UploadFileResponce>(owner.ClientOnly().Client.GetServerAgent(), message);
				return responce.Body.UUID;
			}

			public async Task<FileMetaUIDTO> GetFileMeta(string uuid)
			{
				var message = new PluginProtocolMessage(new LoadFileRequest(uuid, needAddFileContent: false));
				var responce = await owner.SendMessageAndRequestResponse<LoadFileResponce>(owner.ClientOnly().Client.GetServerAgent(), message);
				return new FileMetaUIDTO(responce.Body.FileName, responce.Body.Extension);
			}

			public async Task<string> LoadFile(string uuid)
			{
				var message = new PluginProtocolMessage(new LoadFileRequest(uuid, needAddFileContent: true));
				var responce = await owner.SendMessageAndRequestResponse<LoadFileResponce>(owner.ClientOnly().Client.GetServerAgent(), message, isWaitsAttachment: true);

				var attachment = responce.Message.Attachment ?? throw new NullReferenceException();

				using var memory = new MemoryStream(attachment.Length);
				await attachment.CopyDelegate(memory);
				var rawData = memory.GetBuffer();

				var resource = new PluginResource("file");

				owner.ui.StopResourceShare(resource);
				var address = owner.ui.ShareResource(resource, rawData, "application/octet-stream");

				return address;
			}
		}
	}
}
