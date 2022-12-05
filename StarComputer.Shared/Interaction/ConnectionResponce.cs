namespace StarComputer.Shared.Interaction
{
	public record ConnectionResponce(ConnectionStausCode ErrorCode, string? DebugMessage, object? ResponceBody);
}
