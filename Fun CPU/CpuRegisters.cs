namespace Fun_CPU;


struct CSR
{
    public int value;
    public CpuPrivelege minPrivilege;
    public bool readOnly;
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
    


    public CpuPrivelege Privilege;
    
}