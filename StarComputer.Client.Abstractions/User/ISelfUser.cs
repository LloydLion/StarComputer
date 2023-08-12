using StarComputer.Client.Abstractions.Utils;

namespace StarComputer.Client.Abstractions.User;

public interface ISelfUser : IUser
{
    public void SetMessageHandler(AsyncAction<Message> handler);
}
