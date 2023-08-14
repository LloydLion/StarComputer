namespace StarComputer.Client.Abstractions.Protocol.Machine;

public interface IRemoteMachine : IMachine
{
    public Task SendMessageAsync(Message message);
}
