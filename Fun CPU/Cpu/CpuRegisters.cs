namespace Fun_CPU;

public struct CSR
{
    public int value;
    public CpuPrivelege minPrivilege;
    public bool readOnly;
    
    public CSR(CpuPrivelege minPrivilege, bool readOnly = false)
    {
        this.minPrivilege = minPrivilege;
        this.readOnly = readOnly;
    }
}
public class CpuCSRs
{
    CSR[] csrFile = new CSR[256];
    
    public CSR tvec    { get => csrFile[0];  set => csrFile[0]  = value; } // Trap vector register. Stores the memory address the CPU jumps to when a trap/fault/interrupt occurs.
    public CSR epc     { get => csrFile[1];  set => csrFile[1]  = value; } // Exception program counter. Stores the PC of the instruction that caused the trap so execution can resume later.
    public CSR cause   { get => csrFile[2];  set => csrFile[2]  = value; } // Trap cause register. Stores the reason for the trap/exception/interrupt.
    public CSR tval    { get => csrFile[3];  set => csrFile[3]  = value; } // Trap value register. Stores extra fault information (faulting address, instruction, etc.) in a 32-bit word.
    public CSR status  { get => csrFile[5];  set => csrFile[5]  = value; } // CPU status/control flags register. Stores processor state such as current privilege mode and interrupt state. NOT IMPLEMENTED
    public CSR ie      { get => csrFile[10]; set => csrFile[10] = value; } // Interrupt enable register. If greater than 0, interrupts are treated as enabled.
    public CSR ip      { get => csrFile[11]; set => csrFile[11] = value; } // Interrupt pending register. Indicates whether an interrupt is currently waiting to be handled. NOT IMPLEMENTED
    public CSR satp    { get => csrFile[12]; set => csrFile[12] = value; } // Supervisor address translation and protection register. Controls virtual memory paging and address translation. 
    public CSR scratch { get => csrFile[13]; set => csrFile[13] = value; } // Scratch register for trap handlers or temporary OS/kernel data.
    public CSR cycle   { get => csrFile[14]; set => csrFile[14] = value; } // Cycle counter register. Counts total CPU clock cycles since reset. 
    public CSR instret { get => csrFile[15]; set => csrFile[15] = value; } // Instructions-retired counter. Counts successfully completed instructions. NOT IMPLEMENTED
    public CSR time    { get => csrFile[16]; set => csrFile[16] = value; } // Time register. Stores or exposes a running system timer value. NOT IMPLEMENTED
    

    
    
    public CpuCSRs()
    {
        csrFile[0] = new CSR(CpuPrivelege.User, true);
    }
    
    public void CSRWrite(int index, int value)
    {
        var csr = csrFile[index];
        if (Cpu.instance.privilege < CpuPrivelege.Machine)
        {
            Fault.FaultCpu(CpuTrapCause.IllegalInstruction, 0);
            return;
        }

        if(!csr.readOnly)
            csr.value = value;
    }
    
    public uint CSRRead(int index)
    {
        var csr = csrFile[index];
        if (Cpu.instance.privilege < CpuPrivelege.Machine)
        {
            Fault.FaultCpu(CpuTrapCause.IllegalInstruction, 0);
            return 0;
        }

        return (uint)csr.value;
    }
    
    
    
    
}
public class CpuGPRs
{
    private int[] gpr = new int[32];

    public int this[int index]
    {
        get => gpr[index];
        set => gpr[index] = value;
    }
    
    public uint SP
    {
        get => (uint)gpr[24]; set => gpr[24] = (int)value;
    }
}