using StarComputer.Client.Abstractions.Protocol.Machine;
using StarComputer.Client.Abstractions.Protocol.User;

namespace StarComputer.Client.Abstractions.Plugin;

public interface IPluginMessageHandler
{
	public void Handle(ISelfUser target);

	public void Handle(ISelfMachine target);
}
