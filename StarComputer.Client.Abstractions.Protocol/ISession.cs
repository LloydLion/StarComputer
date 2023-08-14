using StarComputer.Client.Abstractions.Protocol.Machine;
using StarComputer.Client.Abstractions.Protocol.User;

namespace StarComputer.Client.Abstractions.Protocol;

public interface ISession
{
	public IReadOnlyDictionary<Guid, IRemoteMachine> RemoteMachines { get; }

	public ISelfMachine SelfMachine { get; }

	public IReadOnlyDictionary<Guid, IUser> RemoteUsers { get; }

	public ISelfUser SelfUser { get; }


	public IRemoteUser ResolveUseByLogin(string login);
}
