namespace Fun_CPU;

public class CpuRegisters
{
    int[] generalPurposeRegisters = new int[24];
    int[] hypervisorRegisters = new int[6];
    int[] machineRegisters = new int[2];

    public int this[int index]
    {
        get => TryReadRegister(index, out var value) ? value : 0;
        set => TryWriteRegister(index, value);
    }
    
    bool TryReadRegister(int index, out int value)
    {
        if (index < generalPurposeRegisters.Length)
        {
            value = generalPurposeRegisters[index];
            return true;
        }

        index -= generalPurposeRegisters.Length;
        var privilege = (CpuPrivelege)machineRegisters[1];

        if (index < hypervisorRegisters.Length && privilege >= CpuPrivelege.Supervisor)
        {
            value = hypervisorRegisters[index];
            return true;
        }

        if (index < machineRegisters.Length && privilege == CpuPrivelege.Machine)
        {
            value = machineRegisters[index];
            return true;
        }

        value = 0;
        return false;
    }

    void TryWriteRegister(int index, int value)
    {
        if (index < generalPurposeRegisters.Length)
        {
            generalPurposeRegisters[index] = value;
            return;
        }

        index -= generalPurposeRegisters.Length;
        var privilege = (CpuPrivelege)machineRegisters[1];

        if (index < hypervisorRegisters.Length && privilege >= CpuPrivelege.Supervisor)
        {
            hypervisorRegisters[index] = value;
            return;
        }

        if (index < machineRegisters.Length && privilege == CpuPrivelege.Machine)
        {
            machineRegisters[index] = value;
        }
    }
}
