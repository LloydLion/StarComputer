using StarComputer.Common.Abstractions;
using StarComputer.Common.Abstractions.Protocol;
using StarComputer.Common.Abstractions.Protocol.Bodies;
using StarComputer.Common.Protocol.Bodies;
using System.Net;
using System.Net.Http.Headers;
using System.Text;

namespace StarComputer.Common.Protocol
{
	public static class HttpProtocolHelper
	{
		public const string RequestTypeHeader = "Request-Type";
		public const string ConnectionPasswordHeader = "Password";
		public const string ConnectionLoginHeader = "Login";
		public const string CallbackAddressHeader = "Callback-Address";

		private const string DomainHeader = "Domain";
		private const string BodyLengthHeader = "Body-Length";
		private const string BodyEncodingHeader = "Body-Encoding";
		private const string BodyTypeHeader = "Body-Type";
		private const string AttachmentNameHeader = "Attachment-Name";
		private const string AttachmentLengthHeader = "Attachment-Length";
		private const string DebugMessageHeader = "Debug-Message";
		private const string UniqueClientIDHeader = "Unique-Client-ID";

		public static async ValueTask<ProtocolMessage> ParseMessageAsync(HttpListenerContext context, IBodyTypeResolver bodyTypeResolver)
		{
			var headers = context.Request.Headers;

			var domain = headers[DomainHeader];
			if (domain is null)
				throw new BadRequestException($"No {DomainHeader} header in request");

			var bodyLengthRaw = headers[BodyLengthHeader];
			if (bodyLengthRaw is null || int.TryParse(bodyLengthRaw, out var bodyLength) == false)
				throw new BadRequestException($"Invalid {BodyLengthHeader} header");

			var bodyEncodingRaw = headers[BodyEncodingHeader];
			if (bodyEncodingRaw is null)
				throw new BadRequestException($"No {BodyEncodingHeader} header in request");

			var bodyTypeRaw = headers[BodyTypeHeader];
			if (bodyTypeRaw is null)
				throw new BadRequestException("No BodyType header in request");

			var bodyType = bodyTypeResolver.Resolve(new(bodyTypeRaw, domain));

			var buffer = new byte[bodyLength];
			await context.Request.InputStream.ReadAsync(buffer);
			var bodySerialized = Encoding.GetEncoding(bodyEncodingRaw).GetString(buffer);
			var bodyObject = SerializationContext.Instance.Deserialize(bodySerialized, bodyType) ?? throw new NullReferenceException();


			var attachmentName = headers[AttachmentNameHeader];
			ProtocolMessage.MessageAttachment? messageAttachment = null;

			if (attachmentName is not null)
			{
				var attachmentLengthRaw = headers[AttachmentLengthHeader];
				if (attachmentLengthRaw is null || int.TryParse(attachmentLengthRaw, out var attachmentLength) == false)
					throw new BadRequestException($"Invalid {AttachmentLengthHeader} header");

				messageAttachment = new(attachmentName, copyDelegate, attachmentLength);



				ValueTask copyDelegate(Stream stream) => new(context.Request.InputStream.CopyToAsync(stream));
			}


			return new(domain, bodyObject, messageAttachment, headers[DebugMessageHeader]);
		}

		public static async ValueTask<HttpRequestMessage> WriteMessageAsync(ProtocolMessage message, IBodyTypeResolver bodyTypeResolver)
		{
			var httpMessage = new HttpRequestMessage();
			using var content = new MemoryStream();

			httpMessage.Headers.Add(DomainHeader, message.Domain);
			httpMessage.Headers.Add(DebugMessageHeader, message.DebugMessage);


			httpMessage.Headers.Add(BodyEncodingHeader, "UTF-8");
			httpMessage.Headers.Add(BodyTypeHeader, bodyTypeResolver.Code(message.Body.GetType()).PseudoTypeName);

			var serializedBody = SerializationContext.Instance.Serialize(message.Body);
			var bodyBytes = Encoding.UTF8.GetBytes(serializedBody);
			content.Write(bodyBytes);

			httpMessage.Headers.Add(BodyLengthHeader, bodyBytes.Length.ToString());


			var attachment = message.Attachment;

			if (attachment is not null)
			{
				httpMessage.Headers.Add(AttachmentNameHeader, attachment.Name);
				httpMessage.Headers.Add(AttachmentLengthHeader, attachment.Length.ToString());

				await attachment.CopyDelegate(content);
			}

			httpMessage.Content = new ByteArrayContent(content.ToArray());

			return httpMessage;
		}

		public static void PasteClientUniqueID(WebHeaderCollection headers, Guid id)
		{
			headers.Add(UniqueClientIDHeader, id.ToString());
		}

		public static void PasteClientUniqueID(HttpRequestHeaders headers, Guid id)
		{
			headers.Add(UniqueClientIDHeader, id.ToString());
		}

		public static Guid FetchClientUniqueID(HttpListenerContext context)
		{
			var headers = context.Request.Headers;
			var clientID = headers[UniqueClientIDHeader];
			if (clientID is null)
				throw new BadRequestException($"No {UniqueClientIDHeader} in header of message request");
			else if (Guid.TryParse(clientID, out var guid))
				return guid;
			else throw new BadRequestException($"Invalid {UniqueClientIDHeader} in header of message request");
		}

		public static Guid FetchClientUniqueID(HttpResponseMessage responce)
		{
			var headers = responce.Headers;
			var clientID = headers.GetValues(UniqueClientIDHeader).SingleOrDefault();
			if (clientID is null)
				throw new BadRequestException($"No {UniqueClientIDHeader} in header of message request or it is more then one");
			else if (Guid.TryParse(clientID, out var guid))
				return guid;
			else throw new BadRequestException($"Invalid {UniqueClientIDHeader} in header of message request");
		}


		public enum ServerRequestType
		{
			Connect,
			Message,
			Heartbeat,
			Reset
		}

		public enum ClientRequestType
		{
			Message,
			Ping,
			Reset
		}

		public class BadRequestException : HttpException { public BadRequestException(string message) : base(HttpStatusCode.BadRequest, message) { } }

		public class HttpException : Exception
		{
			public HttpStatusCode StatusCode { get; }


			public HttpException(HttpStatusCode statusCode, string message) : base(message)
			{
				StatusCode = statusCode;
			}
		}
	}
}
