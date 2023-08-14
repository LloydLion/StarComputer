namespace StarComputer.Client.Abstractions.Protocol.User;

public interface IUser
{
    public Guid Id { get; }

    public string Login { get; }

    public UserMetadata Metadata { get; }
}
