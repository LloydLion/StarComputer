using StarComputer.Client.Abstractions.Protocol;
using StarComputer.Client.Abstractions.Protocol.User;

namespace StarComputer.Client.Abstractions.Plugin;

public interface IPluginRemoteUser : IUser
{
    public Task SendMessageAsync(Message message);
}
