using StarComputer.Client.Abstractions.Protocol.EventArgs;
using StarComputer.Client.Abstractions.Protocol.Utils;

namespace StarComputer.Client.Abstractions.Protocol.Machine;

public interface ISelfMachine : IMachine
{
    public void SetMessageHandler(AsyncAction<NewMachineMessageEventArgs> handler);
}
