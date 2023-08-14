using StarComputer.Client.Abstractions.Protocol;
using StarComputer.Client.Abstractions.Protocol.Machine;

namespace StarComputer.Client.Abstractions.Plugin;

public interface IPluginRemoteMachine : IMachine
{
    public Task SendMessageAsync(Message message);
}
