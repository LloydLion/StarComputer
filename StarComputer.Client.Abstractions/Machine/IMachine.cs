namespace StarComputer.Client.Abstractions.Machine;

public interface IMachine
{
    public Guid VirtualMachineAddress { get; }

    public MachineMetadata MachineMetadata { get; }
}
