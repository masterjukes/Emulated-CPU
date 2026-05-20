using System.Runtime.Intrinsics.Arm;

namespace Fun_CPU;

public enum CpuPrivelege
{
    User,
    Supervisor,
    Machine
}




public class Cpu
{
    public CpuPrivelege privilege;
    public int flags;
    public int PC;

    public required CpuCSRs controlStatusRegisters;
    public required CpuGPRs registers;
    public required MemoryBus memoryBus;
    
    
    public static Cpu instance = new Cpu
    {
        controlStatusRegisters = new CpuCSRs(),
        registers = new CpuGPRs(),
        memoryBus = new MemoryBus()
    };
    
    int Fetch()
    {
        var data = memoryBus.ReadByte(0);
        return data;
    }
    
    void Decode()
    {
        
    }
    
    void Execute()
    {
        
    }
    
    void WriteBack()
    {
        
    }
    
    
    void HandleTrap(CpuFault fault)
    {
        controlStatusRegisters.epc = controlStatusRegisters.epc with { value = PC };

        controlStatusRegisters.cause = controlStatusRegisters.cause with { value = (int)fault.cause };

        controlStatusRegisters.tval = controlStatusRegisters.tval with { value = fault.info };

        privilege = privilege switch
        {
            CpuPrivelege.User => CpuPrivelege.Supervisor,
            _ => CpuPrivelege.Machine
        };

        PC = controlStatusRegisters.tvec.value;
    }
    
}