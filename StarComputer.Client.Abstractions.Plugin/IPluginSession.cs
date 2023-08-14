using StarComputer.Client.Abstractions.Protocol;

namespace StarComputer.Client.Abstractions.Plugin;

public interface IPluginSession : ISession
{
	public new IReadOnlyDictionary<Guid, IPluginRemoteMachine> RemoteMachines { get; }

	public new IReadOnlyDictionary<Guid, IPluginRemoteUser> RemoteUsers { get; }
}
