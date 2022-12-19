using StarComputer.Common.Abstractions.Plugins.Commands;
using StarComputer.Common.Abstractions.Protocol;

namespace StarComputer.Common.Abstractions.Plugins
{
	public interface IPlugin
	{
		public string Domain { get; }

		public Type TargetUIContextType { get; }


		public void Initialize(IProtocolEnvironment protocolEnviroment, IUIContext uiContext);

		public void LoadCommands(ICommandRepositoryBuilder repository);

		public ValueTask ProcessMessageAsync(ProtocolMessage message, IMessageContext messageContext);

		public ValueTask ProcessCommandAsync(PluginCommandContext commandContext);
	}
}
