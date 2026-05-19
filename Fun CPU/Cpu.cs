using System.Runtime.Intrinsics.Arm;

namespace Fun_CPU;

enum CpuPrivelege
{
    User,
    Supervisor,
    Machine
}

public class Cpu
{
    public required IBus memoryBus;
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
    
}