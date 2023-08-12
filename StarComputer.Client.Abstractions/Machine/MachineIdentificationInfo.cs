namespace StarComputer.Client.Abstractions.Machine;

public record struct MachineIdentificationInfo(Guid VirtualMachineAddress, MachineSecret Secret);
