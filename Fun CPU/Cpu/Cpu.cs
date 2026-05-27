using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics.Arm;

namespace Fun_CPU;

public enum CpuPrivelege
{
    User,
    Machine
}

enum OpCode
{
    //ARITHEMETIC BYTE
    ADD = 0x01,
    SUB = 0x02,
    MUL = 0x03,
    DIV = 0x04,
    MOD = 0x05,

    //LOGIC BYTE
    AND = 0x06,
    OR = 0x07,
    XOR = 0x08,
    NOT = 0x09,
    SHL = 0x0A,
    SHR = 0x0B,

    //ARITHEMETIC DWORD
    ADDL = 0x0C,
    SUBL = 0x0D,
    MULL = 0x0E,
    DIVL = 0x0F,
    MODL = 0x10,

    //LOGIC DWORD
    ANDL = 0x11,
    ORL = 0x12,
    XORL = 0x13,
    NOTL = 0x14,
    SHLL = 0x15,
    SHRL = 0x16,

    //JUMPS AND COMPARES
    CMP = 0x17,
    JMP = 0x18,
    JEQ = 0x19,
    JNE = 0x1A,
    JGT = 0x1B,
    JLT = 0x1C,
    JGE = 0x1D,
    JLE = 0x1E,

    //STACK
    CALL = 0x1F,
    RET = 0x20,
    PUSH = 0x21,
    POP = 0x22,

    //DATA MOVEMENT BYTE
    STORE = 0x23, 
    LOAD = 0x24, 
    MOV = 0x25, 

    //DATA MOVEMENT DWORD
    STORE_L = 0x26, 
    LOAD_L = 0x27, 
    MOV_L = 0x28, 

    //SYSTEM
    NOP = 0x29,
    HALT = 0x2A,

    //INCREMENT AND DECREMENT
    INC = 0x2F, // Increment register/memory
    DEC = 0x30, // Decrement register/memory
    
    //IMMEDIATE JUMPS
    
    JMPI = 0x31,
    JNEI = 0x32,
    JEQI = 0x33,
    JGTI = 0x34,
    JLTI = 0x35,
    JGEI = 0x36,
    JLEI = 0x37,
    
    CMPI = 0x38,
    CALLI = 0x39,
    
    ADDI = 0x3A,
    SUBI = 0x3B,
    MULI = 0x3C,
    DIVI = 0x3D,
    MODI = 0x3E,
    ANDI = 0x3F,
    ORI = 0x40,
    XORI = 0x41,
    NOTI = 0x42,
    SHLI = 0x43,
    SHRI = 0x44,
    
    
    ADDLI = 0x45, 
    SUBLI = 0x46,
    MULLI = 0x47,
    DIVLI = 0x48,
    MODLI = 0x49,
    ANDLI = 0x4A,
    ORLI = 0x4B,
    XORLI = 0x4C,
    NOTLI = 0x4D, 
    SHLLI = 0x4E,  
    SHRLI = 0x4F, 
    
    //PRIVELAGE INSTRUCITONS
    
    CSRW = 0xF0,
    CSRR = 0xF1,
    CSRWI = 0xF2,
    ERET = 0xF3,
    ECALL = 0xF4,
    
    
};

public sealed class Cpu
{
    public CpuPrivelege privilege = CpuPrivelege.Machine;
    public bool[] flags = new bool[32];
    public uint PC;
    bool halted;
    public uint nextPC;

    public required CpuCSRs controlStatusRegisters;
    public required CpuGPRs registers;
    public required MemoryBus memoryBus;
    
    public bool faultPending = false;

    public uint irqPending;
    public uint previousIrqPending; 
    public const uint IRQ_TIMER = 1 << 0;
    public const uint IRQ_KEYBOARD = 1 << 1;
    
    
    byte[] fetchBuffer = new byte[7];
    
    byte[] dataBuffer = new byte[4];
    byte[] dataBuffer2 = new byte[4];
    byte[] dataBuffer3 = new byte[4];


    public static Cpu instance = new Cpu
    {
        controlStatusRegisters = new CpuCSRs(),
        registers = new CpuGPRs(),
        memoryBus = new MemoryBus(),
    };

    public void StepClock()
    {
        if (halted) return;

        try
        {
            Fetch();

            if (faultPending)
                goto fault;
            Execute();

            fault:
            if (faultPending || irqPending != previousIrqPending)
                HandleTrap();
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            Console.WriteLine("CPU faulted!");
            Console.WriteLine("PC: " + PC.ToString("X4"));
            for (int i = 0; i <= 31; i++)
            {
                Console.Write($"R{i}:{registers[i].ToString("X4")} ");
                if (i % 4 == 0) Console.WriteLine();

            }
            Console.WriteLine($"{controlStatusRegisters.ToString()}");
            Console.WriteLine($"{BitConverter.ToString(fetchBuffer)}");
            System.Environment.Exit(1);
            
        }


        PC = nextPC;

        controlStatusRegisters.cycle.value += 1;

    }

    void Fetch()
    {
        fetchBuffer[0] = memoryBus.ReadByte(PC, true);
        fetchBuffer[1] = memoryBus.ReadByte(PC + 1, true);
        fetchBuffer[2] = memoryBus.ReadByte(PC + 2, true);
        fetchBuffer[3] = memoryBus.ReadByte(PC + 3, true);
        fetchBuffer[4] = memoryBus.ReadByte(PC + 4, true);
        fetchBuffer[5] = memoryBus.ReadByte(PC + 5, true);
        fetchBuffer[6] = memoryBus.ReadByte(PC + 6, true);
        
        
        //Console.Write(PC.ToString("X4") + " ");
        //Console.WriteLine(BitConverter.ToString(fetchBuffer));
        
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]

    bool IsValidRegister(byte reg)
    {
        if (reg > 31)
        {
            Console.WriteLine("Invalid register");
            Fault.FaultCpu(CpuTrapCause.IllegalInstruction, reg);
            return false;
        }

        return true;
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]

    void SetReg(byte reg, uint value)
    {
        registers[reg] = (int) value;
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    uint GetReg(byte reg)
    {
        return (uint) registers[reg];
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    void WriteByteToRegister(byte reg, int value)
    {
        registers[reg] = value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    byte ReadByteFromRegister(byte reg)
    {
        return (byte) registers[reg];
    }



    void ChangeFlags(int index)
    {
        Array.Fill(flags, false);
        flags[index] = true;
    }
    


    void Execute()
    {
        var op = (OpCode)fetchBuffer[0];
        var opand1 = fetchBuffer[1];
        var opand2 = fetchBuffer[2];
        var opand3 = fetchBuffer[3];
        
        dataBuffer[0] = fetchBuffer[2];
        dataBuffer[1] = fetchBuffer[3];
        dataBuffer[2] = fetchBuffer[4];
        dataBuffer[3] = fetchBuffer[5];
        
        dataBuffer2[0] = fetchBuffer[1];
        dataBuffer2[1] = fetchBuffer[2];
        dataBuffer2[2] = fetchBuffer[3];
        dataBuffer2[3] = fetchBuffer[4];
        
        dataBuffer3[0] = fetchBuffer[3];
        dataBuffer3[1] = fetchBuffer[4];
        dataBuffer3[2] = fetchBuffer[5];
        dataBuffer3[3] = fetchBuffer[6];
        
        var data3 = (uint) BitConverter.ToInt32(dataBuffer3, 0);
        
        ref var data = ref dataBuffer;
        
        

        switch (op)
        {
            case OpCode.ADDL:
                if (!IsValidRegister(opand1) || !IsValidRegister(opand2) || !IsValidRegister(opand3)) return;
                SetReg(opand1, GetReg(opand2) + GetReg(opand3));
                nextPC += 4;
                break;

            case OpCode.SUBL:
                if (!IsValidRegister(opand1) || !IsValidRegister(opand2) || !IsValidRegister(opand3)) return;
                SetReg(opand1, GetReg(opand2) - GetReg(opand3));
                nextPC += 4;
                break;

            case OpCode.MULL:
                if (!IsValidRegister(opand1) || !IsValidRegister(opand2) || !IsValidRegister(opand3)) return;
                SetReg(opand1, GetReg(opand2) * GetReg(opand3));
                nextPC += 4;
                break;

            case OpCode.DIVL:
                if (!IsValidRegister(opand1) || !IsValidRegister(opand2) || !IsValidRegister(opand3)) return;
                if (GetReg(opand3) == 0) return;
                SetReg(opand1, GetReg(opand2) / GetReg(opand3));
                nextPC += 4;
                break;

            case OpCode.MODL:
                if (!IsValidRegister(opand1) || !IsValidRegister(opand2) || !IsValidRegister(opand3)) return;
                if (GetReg(opand3) == 0) return;
                SetReg(opand1, GetReg(opand2) % GetReg(opand3));
                nextPC += 4;
                break;

            case OpCode.ANDL:
                if (!IsValidRegister(opand1) || !IsValidRegister(opand2) || !IsValidRegister(opand3)) return;
                SetReg(opand1, GetReg(opand2) & GetReg(opand3));
                nextPC += 4;
                break;

            case OpCode.ORL:
                if (!IsValidRegister(opand1) || !IsValidRegister(opand2) || !IsValidRegister(opand3)) return;
                SetReg(opand1, GetReg(opand2) | GetReg(opand3));
                nextPC += 4;
                break;

            case OpCode.XORL:
                if (!IsValidRegister(opand1) || !IsValidRegister(opand2) || !IsValidRegister(opand3)) return;
                SetReg(opand1, GetReg(opand2) ^ GetReg(opand3));
                nextPC += 4;
                break;

            case OpCode.NOTL:
                if (!IsValidRegister(opand1) || !IsValidRegister(opand2)) return;
                SetReg(opand1, ~GetReg(opand2));
                nextPC += 3;
                break;

            case OpCode.SHLL:
                if (!IsValidRegister(opand1) || !IsValidRegister(opand2) || !IsValidRegister(opand3)) return;
                SetReg(opand1, GetReg(opand2) << (int)GetReg(opand3));
                nextPC += 4;
                break;

            case OpCode.SHRL:
                if (!IsValidRegister(opand1) || !IsValidRegister(opand2) || !IsValidRegister(opand3)) return;
                SetReg(opand1, GetReg(opand2) >> (int)GetReg(opand3));
                nextPC += 4;
                break;

            case OpCode.ADD:
                if (!IsValidRegister(opand1) || !IsValidRegister(opand2) || !IsValidRegister(opand3)) return;
                WriteByteToRegister(opand1,  ReadByteFromRegister(opand2) + ReadByteFromRegister(opand3));
                nextPC += 4;
                break;

            case OpCode.SUB:
                if (!IsValidRegister(opand1) || !IsValidRegister(opand2) || !IsValidRegister(opand3)) return;
                WriteByteToRegister(opand1, ReadByteFromRegister(opand2) - ReadByteFromRegister(opand3));
                nextPC += 4;
                break;

            case OpCode.MUL:
                if (!IsValidRegister(opand1) || !IsValidRegister(opand2) || !IsValidRegister(opand3)) return;
                WriteByteToRegister(opand1, ReadByteFromRegister(opand2) * ReadByteFromRegister(opand3));
                nextPC += 4;
                break;

            case OpCode.DIV:
                if (!IsValidRegister(opand1) || !IsValidRegister(opand2) || !IsValidRegister(opand3)) return;
                if (ReadByteFromRegister(opand3) == 0) return;
                WriteByteToRegister(opand1, ReadByteFromRegister(opand2) / ReadByteFromRegister(opand3));
                nextPC += 4;
                break;

            case OpCode.MOD:
                if (!IsValidRegister(opand1) || !IsValidRegister(opand2) || !IsValidRegister(opand3)) return;
                if (ReadByteFromRegister(opand3) == 0) return;
                WriteByteToRegister(opand1, ReadByteFromRegister(opand2) % ReadByteFromRegister(opand3));
                nextPC += 4;
                break;

            case OpCode.AND:
                if (!IsValidRegister(opand1) || !IsValidRegister(opand2) || !IsValidRegister(opand3)) return;
                WriteByteToRegister(opand1, ReadByteFromRegister(opand2) & ReadByteFromRegister(opand3));
                nextPC += 4;
                break;

            case OpCode.OR:
                if (!IsValidRegister(opand1) || !IsValidRegister(opand2) || !IsValidRegister(opand3)) return;
                WriteByteToRegister(opand1, ReadByteFromRegister(opand2) | ReadByteFromRegister(opand3));
                nextPC += 4;
                break;

            case OpCode.XOR:
                if (!IsValidRegister(opand1) || !IsValidRegister(opand2) || !IsValidRegister(opand3)) return;
                WriteByteToRegister(opand1, ReadByteFromRegister(opand2) ^ ReadByteFromRegister(opand3));
                nextPC += 4;
                break;

            case OpCode.NOT:
                if (!IsValidRegister(opand1) || !IsValidRegister(opand2)) return;
                WriteByteToRegister(opand1, ~ReadByteFromRegister(opand2));
                nextPC += 3;
                break;

            case OpCode.SHL:
                if (!IsValidRegister(opand1) || !IsValidRegister(opand2) || !IsValidRegister(opand3)) return;
                WriteByteToRegister(opand1, ReadByteFromRegister(opand2) << ReadByteFromRegister(opand3));
                nextPC += 4;
                break;

            case OpCode.SHR:
                if (!IsValidRegister(opand1) || !IsValidRegister(opand2) || !IsValidRegister(opand3)) return;
                WriteByteToRegister(opand1, ReadByteFromRegister(opand2) >> ReadByteFromRegister(opand3));
                nextPC += 4;
                break;

            case OpCode.CMP:
                nextPC += 3;
                if (!IsValidRegister(opand1) || !IsValidRegister(opand2)) return;
                if (GetReg(opand1) == GetReg(opand2))
                {
                    ChangeFlags(0);
                }
                else if (GetReg(opand1) < GetReg(opand2))
                {
                    ChangeFlags(1);
                }
                else
                {
                    ChangeFlags(2);
                }


                break;

            case OpCode.JMP:
                nextPC += 0;
                if (!IsValidRegister(opand1)) return;
                nextPC = GetReg(opand1);
                break;

            case OpCode.JEQ:
                nextPC += 2;
                if (!IsValidRegister(opand1)) return;
                if (flags[0])
                {
                    nextPC = GetReg(opand1);
                    nextPC += 0;
                }

                break;

            case OpCode.JNE:
                nextPC += 2;
                if (!IsValidRegister(opand1)) return;
                if (!flags[0])
                {
                    nextPC = GetReg(opand1);
                }

                break;

            case OpCode.JGT:
                nextPC += 2;
                if (!IsValidRegister(opand1)) return;
                if (!flags[0] && !flags[1])
                {
                    nextPC = GetReg(opand1);
                }

                break;

            case OpCode.JLT:
                nextPC += 2;
                if (!IsValidRegister(opand1)) return;
                if (flags[1])
                {
                    nextPC = GetReg(opand1);
                    nextPC += 0;
                }
                break;

            case OpCode.JGE:
                nextPC += 2;
                if (!IsValidRegister(opand1)) return;
                if ((flags[0]) || (!flags[0] && !flags[1]))
                {
                    nextPC = GetReg(opand1);
                    nextPC += 0;
                }
                break;

            case OpCode.JLE:
                nextPC += 2;
                if (!IsValidRegister(opand1)) return;
                if (flags[0] || flags[1])
                {
                    nextPC = GetReg(opand1);
                    nextPC += 0;
                }
                break;

            case OpCode.CALL:
                nextPC += 2;
                registers.SP -= 4;
                memoryBus.WriteWord(registers.SP, nextPC);
                nextPC = GetReg(opand1);
                break;

            case OpCode.RET:
                nextPC += 0;
                nextPC = memoryBus.ReadWord(registers.SP);
                registers.SP += 4;
                break;

            case OpCode.PUSH:
                nextPC += 2;
                if (!IsValidRegister(opand1)) return;
                registers.SP -= 4;
                memoryBus.WriteWord(registers.SP, GetReg(opand1));
                break;

            case OpCode.POP:
                nextPC += 2;
                if (!IsValidRegister(opand1)) return;
                SetReg(opand1, memoryBus.ReadWord(registers.SP));
                registers.SP += 4;
                break;

            case OpCode.STORE:
                nextPC += 3;
                if (!IsValidRegister(opand2)) return;
                memoryBus.WriteByte(GetReg(opand1), ReadByteFromRegister(opand2));
                break;

            case OpCode.LOAD:
                nextPC += 3;
                if (!IsValidRegister(opand1) || !IsValidRegister(opand2)) return;
                WriteByteToRegister(opand1, memoryBus.ReadByte(GetReg(opand2)));
                break;

            case OpCode.MOV:
                nextPC += 3;
                if (!IsValidRegister(opand1)) return;
                WriteByteToRegister(opand1, opand2);
                break;

            case OpCode.STORE_L:
                nextPC += 3;
                if (!IsValidRegister(opand1) || !IsValidRegister(opand2)) return;
                memoryBus.WriteWord(GetReg(opand1), GetReg(opand2));
                break;

            case OpCode.LOAD_L:
                nextPC += 3;
                if (!IsValidRegister(opand1) || !IsValidRegister(opand2)) return;
                SetReg(opand1, memoryBus.ReadWord(GetReg(opand2)));
                break;

            case OpCode.MOV_L:
                nextPC += 6;
                if (!IsValidRegister(opand1)) return;
                SetReg(opand1, (uint)BitConverter.ToInt32(data, 0));
                break;

            case OpCode.NOP:
                nextPC += 1;
                break;

            case OpCode.HALT:
                nextPC += 1;
                halted = true;
                break;

            case OpCode.INC:
                nextPC += 2;
                if (!IsValidRegister(opand1)) return;
                SetReg(opand1, GetReg(opand1) + 1);
                break;

            case OpCode.DEC:
                nextPC += 2;
                if (!IsValidRegister(opand1)) return;
                SetReg(opand1, GetReg(opand1) - 1);
                break;
            
            
            case OpCode.JMPI:
                nextPC = BitConverter.ToUInt32(dataBuffer2);
                break;
            
            case OpCode.JEQI:
                nextPC += 5;
                if (flags[0])
                {
                    nextPC = BitConverter.ToUInt32(dataBuffer2);
                    nextPC += 0;
                }

                break;

            case OpCode.JNEI:
                nextPC += 5;
                if (!flags[0])
                {
                    nextPC = BitConverter.ToUInt32(dataBuffer2);
                }
                break;

            case OpCode.JGTI:
                nextPC += 5;
                if (!flags[0] && !flags[1])
                {
                    nextPC = BitConverter.ToUInt32(dataBuffer2);
                }

                break;

            case OpCode.JLTI:
                nextPC += 5;
                if (flags[1])
                {
                    nextPC = BitConverter.ToUInt32(dataBuffer2);

                }
                break;

            case OpCode.JGEI:
                nextPC += 5;
                if (flags[0] || (!flags[0])&& !flags[1])
                {
                    nextPC = BitConverter.ToUInt32(dataBuffer2);
                }
                break;

            case OpCode.JLEI:
                nextPC += 5;
                if (flags[0] || flags[1])
                {
                    nextPC = BitConverter.ToUInt32(dataBuffer2);
                }
                break;
            
            case OpCode.CMPI:
                nextPC += 6;
                var cmpimm = BitConverter.ToUInt32(dataBuffer);
                if (GetReg(opand1) == cmpimm)
                {
                    ChangeFlags(0);
                }
                else if (GetReg(opand1) < cmpimm)
                {
                    ChangeFlags(1);
                }
                else
                {
                    ChangeFlags(2);
                }
                break;
                
            
            case OpCode.CALLI:
                nextPC += 5;
                registers.SP -= 4;
                memoryBus.WriteWord(registers.SP, nextPC);
                nextPC = BitConverter.ToUInt32(dataBuffer2);
                break;
                
                
                
            
            
            case OpCode.CSRR:
                nextPC += 3;
                if (privilege == CpuPrivelege.User)
                {
                    Fault.FaultCpu(CpuTrapCause.IllegalInstruction, (int) op);
                    return;
                }
                SetReg(opand1, controlStatusRegisters.CSRRead(opand2));
                break;
            
            case OpCode.CSRW:
                nextPC += 3;
                if (privilege == CpuPrivelege.User)
                {
                    Fault.FaultCpu(CpuTrapCause.IllegalInstruction, (int) op);
                    return;
                }
                controlStatusRegisters.CSRWrite(opand1, (int) GetReg(opand2));
                break;
            
            case OpCode.CSRWI:
                nextPC += 6;
                if (privilege == CpuPrivelege.User)
                {
                    Fault.FaultCpu(CpuTrapCause.IllegalInstruction, (int) op);
                    return;
                }
                controlStatusRegisters.CSRWrite(opand1, BitConverter.ToInt32(dataBuffer, 0));;
                break;
            
            case OpCode.ECALL:
                controlStatusRegisters.ip.value = (int)InterruptCause.Software; 
                Fault.FaultCpu(CpuTrapCause.EnvironmentCallUser, 0);
                nextPC = PC + 1;
                break;
            
            case OpCode.ERET:
                nextPC += 1;
                if (privilege == CpuPrivelege.User)
                {
                    Fault.FaultCpu(CpuTrapCause.IllegalInstruction, (int) op);
                    return;
                }
                controlStatusRegisters.status.value = (int)SetBit((uint)controlStatusRegisters.status.value, 0, GetBit((uint)controlStatusRegisters.status.value, 1)); 
                privilege = GetBit((uint)controlStatusRegisters.status.value, 2) ? CpuPrivelege.Machine : CpuPrivelege.User;
                nextPC = (uint) controlStatusRegisters.epc.value;
                break;
            
            
            
            case OpCode.ADDI:
                WriteByteToRegister(opand1,  ReadByteFromRegister(opand2) + opand3);
                nextPC += 4;
                break;

            case OpCode.SUBI:
                WriteByteToRegister(opand1, ReadByteFromRegister(opand2) - opand3);
                nextPC += 4;
                break;

            case OpCode.MULI:
                WriteByteToRegister(opand1, ReadByteFromRegister(opand2) * opand3);
                nextPC += 4;
                break;

            case OpCode.DIVI:
                if (opand3 == 0)
                {
                    Fault.FaultCpu(CpuTrapCause.IllegalInstruction, (int) op);
                    return;
                }
                WriteByteToRegister(opand1, ReadByteFromRegister(opand2) / opand3);
                nextPC += 4;
                break;

            case OpCode.MODI:
                if (opand3 == 0)
                {
                    Fault.FaultCpu(CpuTrapCause.IllegalInstruction, (int) op);
                    return;
                }
                WriteByteToRegister(opand1, ReadByteFromRegister(opand2) % opand3);
                nextPC += 4;
                break;

            case OpCode.ANDI:
                WriteByteToRegister(opand1, ReadByteFromRegister(opand2) & opand3);
                nextPC += 4;
                break;

            case OpCode.ORI:
                WriteByteToRegister(opand1, ReadByteFromRegister(opand2) | opand3);
                nextPC += 4;
                break;

            case OpCode.XORI:
                WriteByteToRegister(opand1, ReadByteFromRegister(opand2) ^ opand3);
                nextPC += 4;
                break;

            case OpCode.NOTI:
                WriteByteToRegister(opand1, ~opand2);
                nextPC += 3;
                break;

            case OpCode.SHLI:
                WriteByteToRegister(opand1, ReadByteFromRegister(opand2) << opand3);
                nextPC += 4;
                break;

            case OpCode.SHRI:
                WriteByteToRegister(opand1, ReadByteFromRegister(opand2) >> opand3);
                nextPC += 4;
                break;
            
            
            case OpCode.ADDLI:
                SetReg(opand1, GetReg(opand2) + data3);
                nextPC += 7;
                break;

            case OpCode.SUBLI:
                SetReg(opand1, GetReg(opand2) - data3);
                nextPC += 7;
                break;

            case OpCode.MULLI:
                SetReg(opand1, GetReg(opand2) * data3);
                nextPC += 7;
                break;

            case OpCode.DIVLI:
                if (data3 == 0)
                {
                    Fault.FaultCpu(CpuTrapCause.IllegalInstruction, (int) op);
                    return;
                }
                SetReg(opand1, GetReg(opand2) / data3);
                nextPC += 7;
                break;

            case OpCode.MODLI:
                if (data3 == 0)
                {
                    Fault.FaultCpu(CpuTrapCause.IllegalInstruction, (int) op);
                    return;
                }
                SetReg(opand1, GetReg(opand2) % data3);
                nextPC += 7;
                break;

            case OpCode.ANDLI:
                SetReg(opand1, GetReg(opand2) & data3);
                nextPC += 7;
                break;

            case OpCode.ORLI:
                SetReg(opand1, GetReg(opand2) | data3);
                nextPC += 7;
                break;

            case OpCode.XORLI:
                SetReg(opand1, GetReg(opand2) ^ data3);
                nextPC += 7;
                break;

            case OpCode.NOTLI:
                SetReg(opand1, ~data3);
                nextPC += 6;
                break;

            case OpCode.SHLLI:
                SetReg(opand1, GetReg(opand2) << (int)data3);
                nextPC += 7;
                break;
            
            case OpCode.SHRLI:
                SetReg(opand1, GetReg(opand2) >> (int)data3);
                nextPC += 7;
                break;   
                
                

            default:
                nextPC += 1;
                Console.WriteLine("Unknown opcode: " + op);
                Fault.FaultCpu(CpuTrapCause.IllegalInstruction, (int) op);
                break;
        }
    }
    
    public static bool GetBit(uint value, int bit)
    {
        return (value & (1u << bit)) != 0;
    }
    public static uint SetBit(uint value, int bit, bool bitValue)
    {
        if(bitValue)
            return value | (1u << bit);
        
        return value & ~(1u << bit);
    }
    void HandleTrap()
    {
        faultPending = false;
        const int MPIE_BIT = 1;
        const int MIE_BIT = 0;
        const int MPP_BIT = 2;
        

        
        uint status = (uint)controlStatusRegisters.status.value;

        bool mie = GetBit(status, MIE_BIT);
        bool isInterrupt =
            Fault.cause == CpuTrapCause.Interrupt;
        
        bool isException = !isInterrupt;

        // -----------------------------
        // 1. INTERRUPT HANDLING
        // -----------------------------
        
        
        if (isInterrupt)
        {
            previousIrqPending = irqPending;
            if (!mie)
            {
                Console.WriteLine(controlStatusRegisters.status.value);

                return;
            }
            irqPending = 0;
        }
         
        Console.WriteLine("Handling trap");
        Console.WriteLine("Fault: " + Fault.cause);
        Console.WriteLine(controlStatusRegisters.tval.value);
        Console.WriteLine("IP: " + (uint)controlStatusRegisters.ip.value);
        
        // -----------------------------
        // 2. SAVE CONTEXT (COMMON)
        // -----------------------------

        // Save PC
        controlStatusRegisters.epc.value = (int)nextPC; 

        // Save cause/tval
        controlStatusRegisters.cause.value = (int)Fault.cause;

        controlStatusRegisters.tval.value = Fault.info; 

        // -----------------------------
        // 3. SAVE STATUS (MPIE + MPP)
        // -----------------------------
        


        // MPIE = MIE
        status = SetBit(status, MPIE_BIT, mie);

        // MIE = 0 (disable interrupts in handler)
        status = SetBit(status, MIE_BIT, false);

        // MPP = current privilege
        var oldMpp = GetBit(status, MPP_BIT);
        status = SetBit(status, MPP_BIT, (int)privilege > 0);

        controlStatusRegisters.status.value = (int)status; 

        // -----------------------------
        // 4. SWITCH TO KERNEL MODE
        // -----------------------------
        privilege = CpuPrivelege.Machine;

        
        Console.WriteLine((uint)controlStatusRegisters.tvec.value);
        // Jump to trap handler
        nextPC = (uint)controlStatusRegisters.tvec.value;
    }
}