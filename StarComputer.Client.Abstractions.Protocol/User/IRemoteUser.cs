namespace StarComputer.Client.Abstractions.Protocol.User;

public interface IRemoteUser : IUser
{
    public Task SendMessageAsync(Message message);
}
