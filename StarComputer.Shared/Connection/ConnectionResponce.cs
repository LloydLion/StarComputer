using Newtonsoft.Json.Linq;

namespace StarComputer.Shared.Connection
{
	public record ConnectionResponce(ConnectionStausCode StatusCode, string? DebugMessage, JObject? ResponceBody, string? BodyTypeName);
}
