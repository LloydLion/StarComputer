using System.Security;

namespace StarComputer.Client.Abstractions.Machine;

public interface IManualMachineRegistrationAgent : IMachineRegistrationAgent
{
	public Task<ulong> RequestRegistration(MachineRegistrationDTO registrationDTO, SecureString serverPassword);

	public Task<MachineIdentificationInfo> CompleteRegistration(ulong requestId);
}
