namespace Fun_CPU;

public enum CpuTrapCause
{
    InstructionAddressMisaligned,
    InstructionAccessFault,
    IllegalInstruction,
    Breakpoint,

    LoadAddressMisaligned,
    LoadAccessFault,

    StoreAddressMisaligned,
    StoreAccessFault,

    EnvironmentCallUser,
    EnvironmentCallSupervisor,
    EnvironmentCallMachine,

    InstructionPageFault,
    LoadPageFault,
    StorePageFault
}

public class CpuFault : Exception
{
    public CpuTrapCause cause;
    public int info;
    
    public CpuFault(CpuTrapCause cause, int info)
    {
        this.cause = cause;
        this.info = info;
        
    }

}