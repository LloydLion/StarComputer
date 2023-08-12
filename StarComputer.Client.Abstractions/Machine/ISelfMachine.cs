using StarComputer.Client.Abstractions.Utils;

namespace StarComputer.Client.Abstractions.Machine;

public interface ISelfMachine : IMachine
{
    public void SetMessageHandler(AsyncAction<Message> handler);
}
