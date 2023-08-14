using System.Security;
using StarComputer.Client.Abstractions.Protocol.Bundle;
using StarComputer.Client.Abstractions.Protocol.Machine;
using StarComputer.Client.Abstractions.Protocol.User;

namespace StarComputer.Client.Abstractions.Protocol;

public interface IServer
{
	public ServerMetadata Metadata { get; }

	public IMachineRegistrationAgent? MachineRegistrationAgent { get; }

	public Task<MachineSessionToken> IdentifyMachineAsync(MachineIdentificationInfo identificationInfo);

	public Task<ISession> BeginSessionAsync(MachineSessionToken machine, UserAuthToken userAuth);

	public Task<UserAuthToken> LoginAsync(MachineSessionToken machine, string Login, SecureString password);

	public Task<BundleHash> GetBundleHash();

	public Task<BundleArchive> DownloadArchive();
}
