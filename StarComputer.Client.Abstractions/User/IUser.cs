namespace StarComputer.Client.Abstractions.User;

public interface IUser
{
    public Guid Id { get; }

    public string Login { get; }

    public UserMetadata Metadata { get; }
}
