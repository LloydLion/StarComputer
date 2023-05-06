using Microsoft.Extensions.Localization;
using StarComputer.Common.Abstractions.Plugins;
using StarComputer.Common.Abstractions.Plugins.Persistence;
using StarComputer.Common.Abstractions.Plugins.UI.HTML;
using StarComputer.PluginDevelopmentKit;

namespace QuickChatPlugin
{
	[Plugin("QuickChat")]
	public partial class QuickChatPlugin : PluginBase
	{
		private readonly IHTMLUIContext ui;
		private readonly IPluginPersistenceService persistence;
		private readonly IStringLocalizer localizer;


		public QuickChatPlugin(IProtocolEnvironment environment, IHTMLUIContext ui, IPluginPersistenceService persistence, IStringLocalizer localizer) : base(environment)
		{
			this.ui = ui;
			this.persistence = persistence;
			this.localizer = localizer;
			serverUI = new ServerUIContext(this);
			clientUI = new ClientUIContext(this);
		}


		private abstract class CommonUIContext
		{
			public record FileMetaUIDTO(string FileName, string Extension);
		}

		[MessageBody("chatInitRequest")]
		private class ClientChatInitializationRequest { }

		[MessageBody("chatInit")]
		private class ClientChatInitializationModel
		{
			public IEnumerable<Message> Messages { get; }


			public ClientChatInitializationModel(IEnumerable<Message> messages)
			{
				Messages = messages;
			}
		}

		[MessageBody("messageSend")]
		private class MessageSendPackage
		{
			public Message Message { get; }


			public MessageSendPackage(Message message)
			{
				Message = message;
			}
		}

		[MessageBody("uploadFileRequest")]
		private class UploadFileRequest
		{
			public string FullFileName { get; }


			public UploadFileRequest(string fullFileName)
			{
				FullFileName = fullFileName;
			}
		}

		[MessageBody("uploadFileResponce")]
		private class UploadFileResponce
		{
			public string UUID { get; }


			public UploadFileResponce(string uuid)
			{
				UUID = uuid;
			}
		}

		[MessageBody("loadFileRequest")]
		private class LoadFileRequest
		{
			public string UUID { get; }

			public bool NeedAddFileContent { get; }


			public LoadFileRequest(string uuid, bool needAddFileContent)
			{
				UUID = uuid;
				NeedAddFileContent = needAddFileContent;
			}
		}

		[MessageBody("loadFileResponce")]
		private class LoadFileResponce
		{
			public string FileName { get; }

			public string Extension { get; }


			public LoadFileResponce(string fileName, string extension)
			{
				FileName = fileName;
				Extension = extension;
			}
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
				Type = message.Type.ToString();
			}


			public string Author { get; }

			public string Content { get; }

			public string Type { get; }
		}
	}
}
