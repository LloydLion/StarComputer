namespace StarComputer.Client.Abstractions.Machine;

public interface IRemoteMachine : IMachine
{
    public Task SendMessageAsync(Message message);
}
