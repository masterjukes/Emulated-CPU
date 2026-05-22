namespace Fun_CPU.Vga;

public abstract class MMIODevice
{
    public static List<MMIODevice> devices = new();
    private static int nextBaseAddress = 0;
    public MMIODevice()
    {
        baseAddress = nextBaseAddress;
        nextBaseAddress += size;
        devices.Add(this);
        Console.WriteLine($"Device {this.GetType().Name} at {baseAddress:X8}");
    }
    public int baseAddress; 
    public abstract int size { get; }
    public abstract float updateDeltaTime { get; }
    public virtual void OnConnect(){}
    public virtual void OnDisconnect(){}

    public virtual void UpdateDevice() {}
    
}