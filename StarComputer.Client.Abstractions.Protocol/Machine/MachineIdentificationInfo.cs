namespace StarComputer.Client.Abstractions.Protocol.Machine;

public record struct MachineIdentificationInfo(Guid VirtualMachineAddress, MachineSecret Secret);
