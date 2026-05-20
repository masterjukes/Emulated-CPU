namespace Fun_CPU;


struct CSR
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
public class CpuRegisters
{
    private int[] gpr = new int[24];
    CSR[] csrFile = new CSR[256];
    public int flags;
    public int PC;

    public int this[int index]
    {
        get => gpr[index];
        set => gpr[index] = value;
    }
    
    public int SP
    {
        get => gpr[24]; set => gpr[24] = value;
    }

    public void CSRWrite(int index, int value)
    {
        var csr = csrFile[index];
        if(Privilege < csr.minPrivilege)
            throw new CpuFault(CpuTrapCause.InstructionAccessFault);
        
        if(!csr.readOnly)
            csr.value = value;
    }
    
    public int CSRRead(int index)
    {
        var csr = csrFile[index];
        if(Privilege <  csr.minPrivilege)
            throw new CpuFault(CpuTrapCause.InstructionAccessFault);
        
        return csr.value;
    }

    
    
    
    public CpuPrivelege Privilege;
    
    
    public CpuRegisters()
    {
        for(int i = 0; i < 256; i++)
        {
            csrFile[i] = new CSR();
        }

        csrFile[0] = new CSR(CpuPrivelege.Supervisor, false);
        csrFile[1] = new CSR(CpuPrivelege.Supervisor, false);
        csrFile[2] = new CSR(CpuPrivelege.Supervisor, false);
    }
    
}