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



public class Fault
{
    
    public static int faultCount = 0;
    public static CpuTrapCause cause;
    public static int info;

    public static void FaultCpu(CpuTrapCause cause, int info)
    {
        Cpu.instance.faultPending = true;
        faultCount++;
        Fault.cause = cause;
        Fault.info = info;
    }
    
    
}