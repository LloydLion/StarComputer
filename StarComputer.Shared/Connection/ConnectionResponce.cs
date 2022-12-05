namespace StarComputer.Shared.Connection
{
	public record ConnectionResponce(ConnectionStausCode ErrorCode, string? DebugMessage, object? ResponceBody);
}
