namespace StarComputer.Client.Abstractions.User;

public interface IRemoteUser : IUser
{
    public Task SendMessageAsync(Message message);
}
