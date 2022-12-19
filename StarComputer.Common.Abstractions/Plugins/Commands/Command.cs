namespace StarComputer.Common.Abstractions.Plugins.Commands
{
	public record Command(string Name, IReadOnlyList<CommandArgument> Arguments, IPlugin Plugin, string Description)
	{

	}
}
