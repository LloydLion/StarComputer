namespace StarComputer.Shared.Plugins
{
	public record Command(string Name, IReadOnlyList<CommandArgument> Arguments, string Description, CommandHandler Handler)
	{
		
	}
}
