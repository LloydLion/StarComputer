using StarComputer.Common.Abstractions.Protocol;

namespace StarComputer.Common.Abstractions.Plugins
{
	public interface IPlugin
	{
		public string Domain { get; }


		public void SetupEnviroment(IPluginEnviroment enviroment);

		public IEnumerable<Command> ListCommands();

		public Task HandleMessageAsync(ProtocolMessage message, IMessageContext context);
	}
}
