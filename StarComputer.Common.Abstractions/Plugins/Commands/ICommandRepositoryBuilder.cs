namespace StarComputer.Common.Abstractions.Plugins.Commands
{
	public interface ICommandRepositoryBuilder
	{
		public void BeginPluginInitalize(IPlugin plugin);

		public void EndPluginInitalize();

		public void AddCommand(CommandModel command);

		public void BakeToRepository(ICommandRepository repository);
	}
}
