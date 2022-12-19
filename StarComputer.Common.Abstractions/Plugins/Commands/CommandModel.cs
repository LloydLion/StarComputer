namespace StarComputer.Common.Abstractions.Plugins.Commands
{
	public record CommandModel(string Name, IReadOnlyList<CommandArgument> Arguments, string Description)
	{

	}
}
