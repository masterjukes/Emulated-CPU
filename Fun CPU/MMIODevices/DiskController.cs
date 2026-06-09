namespace Fun_CPU.Vga;

public class DiskController : MMIODevice
{
    private const int diskSize = 1 << 30;
    const int sectorSize = 512;
    const int blockAmount = diskSize / sectorSize;
    
    const int CommandOffset = 0;
    const int LBAOffset = 1;
    const int BufferOffset = 5;
    const int SectorAmtOffset = 9;
    const int StatusOffset = 10;
    const int IRQEnableOffset = 11;
    
    const byte CommandNull = 0;
    const byte CommandRead = 1;
    const byte CommandWrite = 2;
    const byte CommandIdentify = 3;

    private const byte StatusReady = 0;
    private const byte StatusBusy = 1;
    private const byte StatusError = 2;


    private int GetLBA => Cpu.instance.memoryBus.dev[baseAddress + LBAOffset] << 24 | Cpu.instance.memoryBus.dev[baseAddress + LBAOffset + 1] << 16 | Cpu.instance.memoryBus.dev[baseAddress + LBAOffset + 2] << 8 | Cpu.instance.memoryBus.dev[baseAddress + LBAOffset + 3];
    private unsafe byte* GetBuffer 
    {
        get
        {
            var byte1 = Cpu.instance.memoryBus.dev[baseAddress + BufferOffset];
            var byte2 = Cpu.instance.memoryBus.dev[baseAddress + BufferOffset + 1];
            var byte3 = Cpu.instance.memoryBus.dev[baseAddress + BufferOffset + 2];
            var byte4 = Cpu.instance.memoryBus.dev[baseAddress + BufferOffset + 3];
            var bufferAddr = (byte4 << 24) | (byte3 << 16) | (byte2 << 8) | byte1;
            return Cpu.instance.memoryBus.GetAddrPointer((uint)bufferAddr);
        }
    }
    
    private byte GetSectorAmt =>  Cpu.instance.memoryBus.dev[baseAddress + SectorAmtOffset];

    private byte GetStatus
    {
        get { return Cpu.instance.memoryBus.dev[baseAddress + StatusOffset]; }
        set { Cpu.instance.memoryBus.dev[baseAddress + StatusOffset] = value; }
    }

    private byte GetIRQEnable => Cpu.instance.memoryBus.dev[baseAddress + IRQEnableOffset];
    
    
    const string path = "disk.img";

    public override int size => 32;
    public override float updateDeltaTime => 1000;


    public override void WriteByte(uint address, byte value)
    {
        base.WriteByte(address, value);
        
        
        
        if (address == baseAddress + CommandOffset)
        {
            HandleDiskCommands(value);
        }
    }
    
    
    void HandleDiskCommands(byte value)
    {
        //Console.WriteLine("Handling disk commands");    
        if (value == CommandNull || GetStatus == StatusBusy)
            return;
        
        Console.WriteLine($"Handling disk commands {value}");   
        if (value == CommandRead)
        {
            Console.WriteLine("Reading disk");
            ReadDisk();
        }
        else if (value == CommandWrite)
        {
            WriteDisk();
        }
        else if (value == CommandIdentify)
        {
            IdentifyDisk();
        }

        if (GetIRQEnable == 0x80)
        {
            Console.WriteLine("Handling disk interrupt");
            InterruptController.instance.HandleInterrupt((byte)Cpu.IRQ_DISK0);
        }
        else
        {
            Console.WriteLine("Not handling disk interrupt");
            Console.WriteLine("IRQ Enable: " + GetIRQEnable.ToString("X2"));
        }
    }


    unsafe void ReadDisk()
    {
        var lba = GetLBA;
        var sectorAmt = GetSectorAmt;
        var buffer = GetBuffer;

        if (buffer == null)
        {
            GetStatus = StatusError;
            return;
        }

        GetStatus = StatusBusy;

        long offset = lba * sectorSize;
        int bytesToRead = sectorAmt * sectorSize;

        using FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);

        fs.Position = offset;

        fs.ReadExactly(new Span<byte>(buffer, bytesToRead));

        GetStatus = StatusReady;
    }
    unsafe void WriteDisk()
    {
        var lba = GetLBA;
        var sectorAmt = GetSectorAmt;
        var buffer = GetBuffer;

        if (buffer == null)
        {
            GetStatus = StatusError;
            return;
        }

        GetStatus = StatusBusy;

        long offset = lba * sectorSize;
        int bytesToWrite = sectorAmt * sectorSize;

        using FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);

        fs.Position = offset;

        fs.Write(new Span<byte>(buffer, bytesToWrite));

        GetStatus = StatusReady;
    }
    

    unsafe void IdentifyDisk()
    {

        var buffer = GetBuffer;

        if (buffer == null)
        {
            GetStatus = StatusError;
            return;
        }

        GetStatus = StatusBusy;
        
        *buffer = sectorSize >> 0x8 & 0xFF;
        *(buffer + 1) = sectorSize & 0xFF;
        
        *(buffer + 2) = blockAmount >> 24 & 0xFF;
        *(buffer + 3) = blockAmount >> 16 & 0xFF;
        *(buffer + 4) = blockAmount >> 8 & 0xFF;
        *(buffer + 5) = blockAmount & 0xFF;
        
        *(buffer + 6) = (byte)'H';
        *(buffer + 7) = (byte)'D';
        *(buffer + 8) = (byte)'D';
        *(buffer + 9) = (byte)'0';
        *(buffer + 10) = (byte)'1';
        *(buffer + 11) = (byte)0x0;
        

        GetStatus = StatusReady;
    }
    
    
    
}