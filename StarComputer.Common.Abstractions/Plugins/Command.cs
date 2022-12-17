namespace StarComputer.Common.Abstractions.Plugins
{
	public record Command(string Name, IReadOnlyList<CommandArgument> Arguments, string Description, CommandHandler Handler)
	{
		
	}
}
