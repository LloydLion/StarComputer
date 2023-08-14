using StarComputer.Client.Abstractions.Protocol.User;

namespace StarComputer.Client.Abstractions.Protocol.EventArgs;

public sealed class NewUserMessageEventArgs(IRemoteUser sender, Message message) : NewMessageEventArgs(message)
{
	public IRemoteUser Sender { get; } = sender;
}