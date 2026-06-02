namespace Fun_CPU.Vga;

public class InterruptController : MMIODevice
{
    public static InterruptController instance;
    public InterruptController()
    {
        instance = this;
    }
    public override int size => 1;
    public override float updateDeltaTime => 1000;
    
    
    public void HandleInterrupt(byte pendingIRQ)
    {
        Cpu.instance.irqPending |= pendingIRQ;
        Cpu.instance.controlStatusRegisters.ip.value = (int)InterruptCause.External;
        Fault.FaultCpu(CpuTrapCause.Interrupt, 0);
        
        Cpu.instance.memoryBus.dev[baseAddress] = pendingIRQ;
    }
    
}