using Fun_CPU;

public interface IMemoryRegion
{
    byte ReadByte(uint offset);
    void WriteByte(uint offset, byte value);
    
    int ReadWord(uint offset);
    void WriteWord(uint offset, int value);
    
}

public class Ram : IMemoryRegion
{
    private readonly byte[] data;

    public Ram(uint size)
    {
        data = new byte[size];
    }

    public byte ReadByte(uint offset)
    {
        if (offset >= (uint)data.Length)
            throw new CpuFault(CpuTrapCause.LoadAccessFault, (int)offset);

        return data[(int)offset];
    }

    public void WriteByte(uint offset, byte value)
    {
        if (offset >= (uint)data.Length)
            throw new CpuFault(CpuTrapCause.StoreAccessFault, (int)offset);

        data[(int)offset] = value;
    }

    public int ReadWord(uint offset)
    {
        if (offset >= (uint)data.Length)
            throw new CpuFault(CpuTrapCause.LoadAccessFault, (int)offset);
        
        return BitConverter.ToInt32(data, (int)offset);
    }
    
    public void WriteWord(uint offset, int value)
    {
        if (offset >= (uint)data.Length)
            throw new CpuFault(CpuTrapCause.LoadAccessFault, (int)offset);
        
        var bytes = BitConverter.GetBytes(value);
        for (int i = 0; i < bytes.Length; i++)
            data[(int)offset + i] = bytes[i];
    }
}


public class Rom : IMemoryRegion
{
    private readonly byte[] data;

    public Rom(uint size)
    {
        data = new byte[size];
    }

    public byte ReadByte(uint offset)
    {
        if (offset >= (uint)data.Length)
            throw new CpuFault(CpuTrapCause.LoadAccessFault, (int)offset);

        return data[(int)offset];
    }

    public void WriteByte(uint offset, byte value)
    {
        throw new CpuFault(CpuTrapCause.StoreAccessFault, (int)offset);
    }
    
    public int ReadWord(uint offset)
    {
        if (offset >= (uint)data.Length)
            throw new CpuFault(CpuTrapCause.LoadAccessFault, (int)offset);
        
        return BitConverter.ToInt32(data, (int)offset);
    }
    
    public void WriteWord(uint offset, int value)
    {
        throw new CpuFault(CpuTrapCause.StoreAccessFault, (int)offset);
    }
}


public class MmioRegion : IMemoryRegion
{
    private readonly byte[] data;

    public MmioRegion(uint size)
    {
        data = new byte[size];
    }

    public byte ReadByte(uint offset)
    {
        if (offset >= (uint)data.Length)
            throw new CpuFault(CpuTrapCause.LoadAccessFault, (int)offset);

        return data[(int)offset];
    }

    public void WriteByte(uint offset, byte value)
    {
        if (offset >= (uint)data.Length)
            throw new CpuFault(CpuTrapCause.StoreAccessFault, (int)offset);

        data[(int)offset] = value;
    }
    
    public int ReadWord(uint offset)
    {
        if (offset >= (uint)data.Length)
            throw new CpuFault(CpuTrapCause.LoadAccessFault, (int)offset);
        
        return BitConverter.ToInt32(data, (int)offset);
    }
    
    public void WriteWord(uint offset, int value)
    {
        if (offset >= (uint)data.Length)
            throw new CpuFault(CpuTrapCause.LoadAccessFault, (int)offset);
        
        var bytes = BitConverter.GetBytes(value);
        for (int i = 0; i < bytes.Length; i++)
            data[(int)offset + i] = bytes[i];
    }
}


public class MMU
{
    public class PageTableEntry
    {
        public bool valid;
        public bool readable;
        public bool writable;
        public bool executable;
        public uint physicalPage;
    }
    
    private PageTableEntry Decode(uint pteRaw)
    {
        return new PageTableEntry
        {
            valid      = (pteRaw & (1u << 0)) != 0,
            readable   = (pteRaw & (1u << 1)) != 0,
            writable   = (pteRaw & (1u << 2)) != 0,
            executable = (pteRaw & (1u << 3)) != 0,

            physicalPage = (pteRaw >> 10)
        };
    }

    public enum AccessType { Read, Write, Execute }
    public uint Translate(uint vaddr, AccessType accessType)
    {

        uint vpn = vaddr >> 12;
        uint offset = vaddr & 0xFFF;

        uint root = (uint)(Cpu.instance.controlStatusRegisters.satp.value & 0xFFFFF) << 12;

        uint pteAddr = root + vpn * 4;

        uint pteRaw = (uint) Cpu.instance.memoryBus.ReadWord(pteAddr);

        PageTableEntry pte = Decode(pteRaw);

        if (!pte.valid)
            throw new CpuFault(CpuTrapCause.LoadPageFault, (int)vaddr);
        
        
        if (accessType == AccessType.Write && !pte.writable)
            throw new CpuFault(CpuTrapCause.StoreAccessFault, (int)vaddr);

        if (accessType == AccessType.Read && !pte.readable)
            throw new CpuFault(CpuTrapCause.LoadAccessFault, (int)vaddr);

        if (accessType == AccessType.Execute && !pte.executable)
            throw new CpuFault(CpuTrapCause.InstructionAccessFault, (int)vaddr);

        return (pte.physicalPage << 12) | offset;
    }
}


public class MemoryBus
{
    private struct Region
    {
        public uint start;
        public uint end;    
        public IMemoryRegion device;
    }

    private readonly List<Region> regions = new List<Region>()
    {
        new Region { start = 0x00000000u, end = 1u << 26, device = new Ram(1u << 26) },
        new Region { start = 0x7FFF0000u, end = (uint) (0x7FFF0000u + (1u << 20)), device = new Rom(1u << 20) },
        new Region { start = 0xA0000000u, end = 0xA0000000 + (1u << 12), device = new MmioRegion(1u << 12) }
    };

    IMemoryRegion Resolve(uint addr, out uint offset, bool isLoad)
    {
        foreach (var r in regions)
        {
            if (addr >= r.start && addr < r.end)
            {
                offset = addr - r.start;
                return r.device;
            }
        }

        throw new CpuFault(
            isLoad ? CpuTrapCause.LoadAccessFault : CpuTrapCause.StoreAccessFault,
            (int)addr
        );
    }
    
    private readonly MMU mmu = new();

    public MMU MMU => mmu;

    
    


    public byte ReadByte(uint vaddr, bool willExecute = false)
    {
        uint paddr = mmu.Translate(vaddr, willExecute ? MMU.AccessType.Execute : MMU.AccessType.Read);
        var dev = Resolve(paddr, out uint off, true);
        return dev.ReadByte(off);
    }

    public void WriteByte(uint vaddr, byte value)
    {
        uint paddr = mmu.Translate(vaddr, MMU.AccessType.Write);
        var dev = Resolve(paddr, out uint off, false);
        dev.WriteByte(off, value);
    }

    public uint ReadWord(uint vaddr, bool willExecute = false)
    {
        uint paddr = mmu.Translate(vaddr, willExecute ? MMU.AccessType.Execute : MMU.AccessType.Read);
        var dev = Resolve(paddr, out uint off, true);
        return (uint)dev.ReadWord(off);
    }
    
    public void WriteWord(uint vaddr, uint value)
    {
        uint paddr = mmu.Translate(vaddr, MMU.AccessType.Write);
        var dev = Resolve(paddr, out uint off, false);
        dev.WriteWord(off, (int)value);
    }
}