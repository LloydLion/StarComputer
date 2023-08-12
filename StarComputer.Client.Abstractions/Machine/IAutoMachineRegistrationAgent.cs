using System.Security;

namespace StarComputer.Client.Abstractions.Machine;

public interface IAutoMachineRegistrationAgent
{
	public Task<MachineIdentificationInfo> CompleteRegistration(MachineRegistrationDTO registrationDTO, SecureString serverPassword);
}
