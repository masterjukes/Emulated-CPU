namespace Fun_CPU.Vga;

public abstract class MMIODevice
{
    private static int nextBaseAddress = 0;
    public MMIODevice()
    {
        baseAddress = nextBaseAddress;
        nextBaseAddress += size;
    }
    public int baseAddress; 
    public abstract int size { get; }
    public abstract float updateDeltaTime { get; }
    public virtual void OnConnect(){}
    public virtual void OnDisconnect(){}

    public virtual void UpdateDevice() {}
    
}