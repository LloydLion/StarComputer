using StarComputer.Client.Abstractions.Protocol.Machine;

namespace StarComputer.Client.Abstractions.Protocol.EventArgs;

public sealed class NewMachineMessageEventArgs(IRemoteMachine sender, Message message) : NewMessageEventArgs(message)
{
	public IRemoteMachine Sender { get; } = sender;
}
