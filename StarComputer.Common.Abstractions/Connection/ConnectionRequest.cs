namespace StarComputer.Common.Abstractions.Connection
{
	public record ConnectionRequest(string Login, string ServerPassword, Version ProtocolVersion, IReadOnlyDictionary<string, Version> Plugins);
}
