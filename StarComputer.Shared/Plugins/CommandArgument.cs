namespace StarComputer.Shared.Plugins
{
	public record CommandArgument(string Name, CommandArgument.Type ArgumentType, string Description)
	{
		public enum Type
		{
			String
		}
	}
}
