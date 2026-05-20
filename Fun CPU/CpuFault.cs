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
    public CpuFault(CpuTrapCause cause)
    {
        
    }

}