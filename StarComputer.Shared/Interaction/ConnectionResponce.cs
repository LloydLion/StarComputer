namespace StarComputer.Shared.Interaction
{
	public record ConnectionResponce(ProtocolStausCode ErrorCode, string? DebugMessage, object? ResponceBody);
}
