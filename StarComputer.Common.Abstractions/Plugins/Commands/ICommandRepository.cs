namespace StarComputer.Common.Abstractions.Plugins.Commands
{
	public interface ICommandRepository : IReadOnlyCollection<Command>, IReadOnlyDictionary<string, Command>
	{
		public bool IsInitialized { get; }


		public void Initialize(IEnumerable<Command> commands);
	}
}