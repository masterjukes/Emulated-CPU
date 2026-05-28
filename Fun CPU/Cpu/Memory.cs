using System;
using System.IO;
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
            Fault.FaultCpu(CpuTrapCause.LoadPageFault, (int)vaddr);
        
        
        if (accessType == AccessType.Write && !pte.writable)
            Fault.FaultCpu(CpuTrapCause.StoreAccessFault, (int)vaddr);

        if (accessType == AccessType.Read && !pte.readable)
            Fault.FaultCpu(CpuTrapCause.LoadAccessFault, (int)vaddr);

        if (accessType == AccessType.Execute && !pte.executable)
            Fault.FaultCpu(CpuTrapCause.InstructionAccessFault, (int)vaddr);

        return (pte.physicalPage << 12) | offset;
    }
}


public sealed class MemoryBus
{
    
    private readonly MMU mmu = new();

    public MMU MMU => mmu;

    byte[] ram = new byte[64 * 1024 * 1024];
    byte[] rom = new byte[0x800FFFFF - 0x7FFF0000];
    public byte[] dev = new byte[24 * 1024 * 1024];
    
    public MemoryBus()
    {
        string romPath = Path.Combine(AppContext.BaseDirectory, "rom.bin");
        if (!File.Exists(romPath))
            throw new FileNotFoundException(
                $"CPU ROM image not found at '{romPath}'. " +
                "Build it with: python Assembly/basicassembler.py Assembly/clogs rom.bin",
                romPath);

        var romData = File.ReadAllBytes(romPath);
        if (romData.Length > rom.Length)
            throw new InvalidOperationException(
                $"rom.bin is {romData.Length} bytes but ROM region only fits {rom.Length} bytes.");

        Array.Copy(romData, rom, romData.Length);
    }
    

    
    public byte ReadByte(uint vaddr, bool willExecute = false)
    {
        uint paddr = mmu.Translate(vaddr, willExecute ? MMU.AccessType.Execute : MMU.AccessType.Read);
        
        var ramRegion = paddr < 0x4000000;
        var romRegion = paddr >= 0x7FFF0000 && paddr < 0x800FFFFF;
        var devRegion = paddr >= 0xF0000000 && paddr < 0xFFFFFFFF;
        
        if(ramRegion)
            return ram[paddr];
        if(romRegion)
            return rom[paddr - 0x7FFF0000];
        if(devRegion)
            return dev[paddr - 0xF0000000];
        
        Fault.FaultCpu(CpuTrapCause.LoadAccessFault, (int)vaddr);
        return 0;
    }

    public void WriteByte(uint vaddr, byte value)
    {
        uint paddr = mmu.Translate(vaddr, MMU.AccessType.Write);
        
        var ramRegion = paddr < 0x4000000;
        var romRegion = paddr >= 0x7FFF0000 && paddr < 0x800FFFFF;
        var devRegion = paddr >= 0xF0000000 && paddr < 0xFFFFFFFF;
        
        if(ramRegion)
            ram[paddr] = value;
        if (devRegion)
        {
            dev[paddr - 0xF0000000] = value;
            foreach (var i in MMIODevice.devices)
                i.WriteByte(paddr - 0xF0000000, value);
            
        }

        if(romRegion)
            Fault.FaultCpu(CpuTrapCause.StoreAccessFault, (int)vaddr);
;
    }

    public uint ReadWord(uint vaddr, bool willExecute = false)
    {
        uint paddr = mmu.Translate(vaddr, willExecute ? MMU.AccessType.Execute : MMU.AccessType.Read);
        
        var ramRegion = paddr < 0x4000000;
        var romRegion = paddr >= 0x7FFF0000 && paddr < 0x800FFFFF;
        var devRegion = paddr >= 0xF0000000 && paddr < 0xFFFFFFFF;
        
        if(ramRegion)
            return ram[paddr] | (uint)ram[paddr + 1] << 8 | (uint)ram[paddr + 2] << 16 | (uint)ram[paddr + 3] << 24;
        if(romRegion)
            return rom[paddr - 0x7FFF0000] | (uint)rom[paddr - 0x7FFF0000 + 1] << 8 | (uint)rom[paddr - 0x7FFF0000 + 2] << 16 | (uint)rom[paddr - 0x7FFF0000 + 3] << 24;
        if (devRegion)
        {
            foreach (var i in MMIODevice.devices)
            {
                i.ReadByte(paddr - 0xF0000000);
            }
            return dev[paddr - 0xF0000000] | (uint)dev[paddr - 0xF0000000 + 1] << 8 |
                   (uint)dev[paddr - 0xF0000000 + 2] << 16 | (uint)dev[paddr - 0xF0000000 + 3] << 24;
        }

        Fault.FaultCpu(CpuTrapCause.LoadAccessFault, (int)vaddr);
        return 0;
    }
    
    public uint ReadWordPhys(uint paddr)
    {
        var ramRegion = paddr < 0x4000000;
        var romRegion = paddr >= 0x7FFF0000 && paddr < 0x800FFFFF;
        var devRegion = paddr >= 0xF0000000 && paddr < 0xFFFFFFFF;
        
        if(ramRegion)
            return ram[paddr] | (uint)ram[paddr + 1] << 8 | (uint)ram[paddr + 2] << 16 | (uint)ram[paddr + 3] << 24;
        if(romRegion)
            return rom[paddr - 0x7FFF0000] | (uint)rom[paddr - 0x7FFF0000 + 1] << 8 | (uint)rom[paddr - 0x7FFF0000 + 2] << 16 | (uint)rom[paddr - 0x7FFF0000 + 3] << 24;
        if(devRegion)
            return dev[paddr - 0xF0000000] | (uint)dev[paddr - 0xF0000000 + 1] << 8 | (uint)dev[paddr - 0xF0000000 + 2] << 16 | (uint)dev[paddr - 0xF0000000 + 3] << 24    ;
        
        Fault.FaultCpu(CpuTrapCause.LoadAccessFault, (int)paddr);
        return 0;
    }
    
    public void WriteWord(uint vaddr, uint value)
    {
        uint paddr = mmu.Translate(vaddr, MMU.AccessType.Write);
        
        var ramRegion = paddr < 0x4000000;
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
        
        if (romRegion)
            Fault.FaultCpu(CpuTrapCause.StoreAccessFault, (int)vaddr);
       
    }
}