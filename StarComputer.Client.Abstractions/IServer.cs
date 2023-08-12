using System.Security;
using StarComputer.Client.Abstractions.Machine;
using StarComputer.Client.Abstractions.User;

namespace StarComputer.Client.Abstractions;

public interface IServer
{
	public ServerMetadata Metadata { get; }

	public IMachineRegistrationAgent MachineRegistrationAgent { get; }

	public Task<MachineSessionToken> IdentifyMachineAsync(MachineIdentificationInfo identificationInfo);

	public Task<ISession> BeginSessionAsync(MachineSessionToken machine, UserAuthToken userAuth);

	public Task<UserAuthToken> LoginAsync(MachineSessionToken machine, string Login, SecureString password);

	//TODO: add persistence management
	/*
	 * aka
	 * IDataAgent GetDataAgent(UserAuthToken userAuth)
	 * 
	 * interface IDataAgent:
	 * - Create
	 * - Delete
	 * - Read
	 * - Update
	 * 
	 */

	//TODO: add plugin check and downloading
	/*
	 * aka
	 * PluginVerifyData ListPluginsAsync()
	 * PluginVerifyData:
	 * - Id
	 * - Version
	 * - Hash
	 * 
	 * PluginBundle DownloadPluginsAsync();
	 * 
	 */
}
