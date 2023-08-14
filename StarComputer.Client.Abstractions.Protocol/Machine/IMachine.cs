namespace StarComputer.Client.Abstractions.Protocol.Machine;

public interface IMachine
{
    public Guid VirtualMachineAddress { get; }

    public MachineMetadata MachineMetadata { get; }
}
