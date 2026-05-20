namespace Fun_CPU;

public struct CSR
{
    public int value;
    public CpuPrivelege minPrivilege;
    public bool readOnly;
    
    public CSR(CpuPrivelege minPrivilege, bool readOnly)
    {
        this.minPrivilege = minPrivilege;
        this.readOnly = readOnly;
    }
}
public class CpuCSRs
{
    CSR[] csrFile = new CSR[256];
    
    public CSR tvec {get => csrFile[0]; set => csrFile[0] = value;}
    public CSR epc {get => csrFile[1]; set => csrFile[1] = value;}
    public CSR cause {get => csrFile[2]; set => csrFile[2] = value;}
    public CSR tval {get => csrFile[3]; set => csrFile[3] = value;}
    public CSR status {get => csrFile[5]; set => csrFile[5] = value;}
    public CSR ie {get => csrFile[10]; set => csrFile[10] = value;}
    public CSR ip {get => csrFile[11]; set => csrFile[11] = value;}
    public CSR satp {get => csrFile[12]; set => csrFile[12] = value;}
    public CSR scratch {get => csrFile[13]; set => csrFile[13] = value;}
    public CSR cycle {get => csrFile[14]; set => csrFile[14] = value;}
    public CSR instret {get => csrFile[15]; set => csrFile[15] = value;}
    public CSR time {get => csrFile[16]; set => csrFile[16] = value;}
    

    
    
    public CpuCSRs()
    {
        csrFile[0] = new CSR(CpuPrivelege.User, true);
    }
    
    public void CSRWrite(int index, int value)
    {
        var csr = csrFile[index];
        if(Cpu.instance.privilege < csr.minPrivilege)
            throw new CpuFault(CpuTrapCause.InstructionAccessFault, 0);
        
        if(!csr.readOnly)
            csr.value = value;
    }
    
    public int CSRRead(int index)
    {
        var csr = csrFile[index];
        if(Cpu.instance.privilege <  csr.minPrivilege)
            throw new CpuFault(CpuTrapCause.InstructionAccessFault, 0);
        
        return csr.value;
    }
    
    
    
    
}
public class CpuGPRs
{
    private int[] gpr = new int[24];

    public int this[int index]
    {
        get => gpr[index];
        set => gpr[index] = value;
    }
    
    public int SP
    {
        get => gpr[24]; set => gpr[24] = value;
    }
}