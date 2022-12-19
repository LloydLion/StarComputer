using StarComputer.Common.Abstractions.Plugins;
using StarComputer.Common.Abstractions.Plugins.Commands;

namespace StarComputer.Common.Plugins.Commands
{
	public class CommandRespositoryBuilder : ICommandRepositoryBuilder
	{
		private readonly LinkedList<Command> commands = new();
		private IPlugin? plugin = null;


		public void AddCommand(CommandModel command)
		{
			if (plugin is null)
				throw new InvalidOperationException("Plugin is not initializing now");
			commands.AddLast(new Command(command.Name, command.Arguments, plugin, command.Description));
		}

		public void BeginPluginInitalize(IPlugin plugin)
		{
			if (this.plugin is not null)
				throw new InvalidOperationException($"Plugin with domain {this.plugin.Domain} is initializing now");
			this.plugin = plugin;
		}

		public void EndPluginInitalize()
		{
			if (plugin is null)
				throw new InvalidOperationException("Plugin is not initializing now");
			plugin = null;
		}

		public void BakeToRepository(ICommandRepository repository)
		{
			repository.Initialize(commands);
		}
	}
}
