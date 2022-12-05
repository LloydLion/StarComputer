namespace StarComputer.Shared.Connection
{
	public record ConnectionRequest(string Login, string ServerPassword, Version ProtocolVersion);
}
