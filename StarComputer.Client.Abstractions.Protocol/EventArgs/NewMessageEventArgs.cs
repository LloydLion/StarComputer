namespace StarComputer.Client.Abstractions.Protocol.EventArgs;

public abstract class NewMessageEventArgs(Message message) : System.EventArgs
{
    public Message Message { get; } = message;
}