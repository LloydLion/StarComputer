using StarComputer.Client.Abstractions.Machine;
using StarComputer.Client.Abstractions.Plugin;
using StarComputer.Client.Abstractions.User;

namespace StarComputer.Client.Abstractions;

public interface ISession
{
	public IReadOnlyDictionary<Guid, IRemoteMachine> RemoteMachines { get; }

	public ISelfMachine SelfMachine { get; }

	public IReadOnlyDictionary<Guid, IUser> RemoteUser { get; }

	public IUser SelfUser { get; }

	public IPlugin? CurrentPluginContext { get; }


	public IRemoteUser ResolveUseByLogin(string login);

	public IDisposable SetPluginContext(IPlugin plugin);
}
