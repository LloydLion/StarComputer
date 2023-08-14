using StarComputer.Client.Abstractions.Protocol.EventArgs;
using StarComputer.Client.Abstractions.Protocol.Utils;

namespace StarComputer.Client.Abstractions.Protocol.User;

public interface ISelfUser : IUser
{
    public void SetMessageHandler(AsyncAction<NewUserMessageEventArgs> handler);
}
