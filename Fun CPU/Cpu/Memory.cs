using Fun_CPU;
using Fun_CPU.Vga;


public sealed class MMU
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

        if(Cpu.instance.controlStatusRegisters.satp.value == 0)
            return vaddr;
        
        uint vpn = vaddr >> 12;
        uint offset = vaddr & 0xFFF;

        uint root = (uint)(Cpu.instance.controlStatusRegisters.satp.value & 0xFFFFF) << 12;

        uint pteAddr = root + vpn * 4;

        uint pteRaw = (uint) Cpu.instance.memoryBus.ReadWordPhys(pteAddr);

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


public sealed class MemoryBus
{
    
    private readonly MMU mmu = new();

    public MMU MMU => mmu;

    byte[] ram = new byte[64 * 1024 * 1024];
    byte[] rom = new byte[1 * 1024 * 1024];
    public byte[] dev = new byte[1 * 1024 * 1024];
    

    
    public byte ReadByte(uint vaddr, bool willExecute = false)
    {
        uint paddr = mmu.Translate(vaddr, willExecute ? MMU.AccessType.Execute : MMU.AccessType.Read);
        
        var ramRegion = paddr > 0 && paddr < 0x4000000;
        var romRegion = paddr >= 0x7FFF0000 && paddr < 0x800FFFFF;
        var devRegion = paddr >= 0xF0000000 && paddr < 0xFFFFFFFF;
        
        if(ramRegion)
            return ram[paddr];
        if(romRegion)
            return rom[paddr - 0x7FFF0000];
        if(devRegion)
            return dev[paddr - 0xF0000000];
        
        throw new CpuFault(CpuTrapCause.LoadAccessFault, (int)vaddr);
    }

    public void WriteByte(uint vaddr, byte value)
    {
        uint paddr = mmu.Translate(vaddr, MMU.AccessType.Write);
        
        var ramRegion = paddr > 0 && paddr < 0x4000000;
        var romRegion = paddr >= 0x7FFF0000 && paddr < 0x800FFFFF;
        var devRegion = paddr >= 0xF0000000 && paddr < 0xFFFFFFFF;
        
        if(ramRegion)
            ram[paddr] = value;
        if(devRegion)
            dev[paddr - 0xF0000000] = value;;
        
        throw new CpuFault(CpuTrapCause.StoreAccessFault, (int)vaddr);
;
    }

    public uint ReadWord(uint vaddr, bool willExecute = false)
    {
        uint paddr = mmu.Translate(vaddr, willExecute ? MMU.AccessType.Execute : MMU.AccessType.Read);
        
        var ramRegion = paddr > 0 && paddr < 0x4000000;
        var romRegion = paddr >= 0x7FFF0000 && paddr < 0x800FFFFF;
        var devRegion = paddr >= 0xF0000000 && paddr < 0xFFFFFFFF;
        
        if(ramRegion)
            return ram[paddr] | (uint)ram[paddr + 1] << 8 | (uint)ram[paddr + 2] << 16 | (uint)ram[paddr + 3] << 24;
        if(romRegion)
            return rom[paddr - 0x7FFF0000] | (uint)rom[paddr - 0x7FFF0000 + 1] << 8 | (uint)rom[paddr - 0x7FFF0000 + 2] << 16 | (uint)rom[paddr - 0x7FFF0000 + 3] << 24;
        if(devRegion)
            return dev[paddr - 0xF0000000] | (uint)dev[paddr - 0xF0000000 + 1] << 8 | (uint)dev[paddr - 0xF0000000 + 2] << 16 | (uint)dev[paddr - 0xF0000000 + 3] << 24    ;
        
        throw new CpuFault(CpuTrapCause.LoadAccessFault, (int)vaddr);

    }
    
    public uint ReadWordPhys(uint paddr)
    {
        var ramRegion = paddr > 0 && paddr < 0x4000000;
        var romRegion = paddr >= 0x7FFF0000 && paddr < 0x800FFFFF;
        var devRegion = paddr >= 0xF0000000 && paddr < 0xFFFFFFFF;
        
        if(ramRegion)
            return ram[paddr] | (uint)ram[paddr + 1] << 8 | (uint)ram[paddr + 2] << 16 | (uint)ram[paddr + 3] << 24;
        if(romRegion)
            return rom[paddr - 0x7FFF0000] | (uint)rom[paddr - 0x7FFF0000 + 1] << 8 | (uint)rom[paddr - 0x7FFF0000 + 2] << 16 | (uint)rom[paddr - 0x7FFF0000 + 3] << 24;
        if(devRegion)
            return dev[paddr - 0xF0000000] | (uint)dev[paddr - 0xF0000000 + 1] << 8 | (uint)dev[paddr - 0xF0000000 + 2] << 16 | (uint)dev[paddr - 0xF0000000 + 3] << 24    ;
        
        throw new CpuFault(CpuTrapCause.LoadAccessFault, (int)paddr);
    }
    
    public void WriteWord(uint vaddr, uint value)
    {
        uint paddr = mmu.Translate(vaddr, MMU.AccessType.Write);
        
        var ramRegion = paddr > 0 && paddr < 0x4000000;
        var romRegion = paddr >= 0x7FFF0000 && paddr < 0x800FFFFF;
        var devRegion = paddr >= 0xF0000000 && paddr < 0xFFFFFFFF;

        var bytes = BitConverter.GetBytes(value);
        
        if (ramRegion)
        {
            ram[paddr] = bytes[0];
            ram[paddr + 1] = bytes[1];
            ram[paddr + 2] = bytes[2];
            ram[paddr + 3] = bytes[3];
        }

        if (devRegion)
        {
            dev[paddr - 0xF0000000] = bytes[0];
            dev[paddr - 0xF0000000 + 1] = bytes[1];
            dev[paddr - 0xF0000000 + 2] = bytes[2];
            dev[paddr - 0xF0000000 + 3] = bytes[3];
        }
        
        throw new CpuFault(CpuTrapCause.StoreAccessFault, (int)vaddr);
       
    }
}