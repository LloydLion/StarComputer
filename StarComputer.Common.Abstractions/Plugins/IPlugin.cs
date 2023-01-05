using StarComputer.Common.Abstractions.Plugins.Commands;
using StarComputer.Common.Abstractions.Protocol;
using StarComputer.Common.Abstractions.Protocol.Bodies;

namespace StarComputer.Common.Abstractions.Plugins
{
	public interface IPlugin
	{
		public string Domain { get; }

		public IReadOnlyCollection<Type> TargetUIContextTypes { get; }

		public Version Version { get; }


		public void InitializeAndBuild(IProtocolEnvironment protocolEnviroment, IUIContext uiContext, ICommandRepositoryBuilder commandsBuilder, IBodyTypeResolverBuilder resolverBuilder);

		public ValueTask ProcessMessageAsync(ProtocolMessage message, IMessageContext messageContext);

		public ValueTask ProcessCommandAsync(PluginCommandContext commandContext);
	}
}
