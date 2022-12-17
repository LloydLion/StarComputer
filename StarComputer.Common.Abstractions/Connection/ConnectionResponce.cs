using Newtonsoft.Json.Linq;

namespace StarComputer.Common.Abstractions.Connection
{
	public record ConnectionResponce(ConnectionStausCode StatusCode, string? DebugMessage, JObject? ResponceBody, string? BodyTypeName);
}
