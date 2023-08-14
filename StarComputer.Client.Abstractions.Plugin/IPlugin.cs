using StarComputer.Client.Abstractions.Protocol;

namespace StarComputer.Client.Abstractions.Plugin;

public interface IPlugin
{
	public Task InitializeAsync(ISession session);

	public Task HandleUserMessageAsync(Message userMessage, IPluginRemoteUser sender);

	public Task HandleMachineMessageAsync(Message machineMessage, IPluginRemoteMachine sender);
}
