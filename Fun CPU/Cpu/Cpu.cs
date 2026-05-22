using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics.Arm;

namespace Fun_CPU;

public enum CpuPrivelege
{
    User,
    Supervisor,
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
    STORE = 0x23, //MOV BYTE PTR [%0], %1
    LOAD = 0x24, //MOV BYTE PTR %0, [%1]
    MOV = 0x25, //MOV %0, #4142

    //DATA MOVEMENT DWORD
    STORE_L = 0x26, //MOV DWORD PTR [%0], %1
    LOAD_L = 0x27, //MOV DWORD PTR %0, [%1]
    MOV_L = 0x28, //MOV %0, #41424344

    //SYSTEM
    NOP = 0x29,
    HALT = 0x2A,

    //INCREMENT AND DECREMENT
    INC = 0x2F, // Increment register/memory
    DEC = 0x30, // Decrement register/memory
};

public sealed class Cpu
{
    public CpuPrivelege privilege;
    public bool[] flags = new bool[32];
    public uint PC;
    bool halted;

    public required CpuCSRs controlStatusRegisters;
    public required CpuGPRs registers;
    public required MemoryBus memoryBus;
    
    public bool faultPending = false;
    int pcUpdate = 0;
    
    
    byte[] fetchBuffer = new byte[6];
    
    byte[] dataBuffer = new byte[4];


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
            if (faultPending)
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


        PC += (uint) pcUpdate;

        controlStatusRegisters.cycle =
            controlStatusRegisters.cycle with { value = controlStatusRegisters.cycle.value + 1 };
        
    }

    void Fetch()
    {
        fetchBuffer[0] = memoryBus.ReadByte(PC, true);
        fetchBuffer[1] = memoryBus.ReadByte(PC + 1, true);
        fetchBuffer[2] = memoryBus.ReadByte(PC + 2, true);
        fetchBuffer[3] = memoryBus.ReadByte(PC + 3, true);
        fetchBuffer[4] = memoryBus.ReadByte(PC + 4, true);
        fetchBuffer[5] = memoryBus.ReadByte(PC + 5, true);
        
        
        //Console.Write(PC.ToString("X4") + " ");
        //Console.WriteLine(BitConverter.ToString(fetchBuffer));
        
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]

    bool IsValidRegister(byte reg)
    {
        if (reg > 31)
        {
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



    void ChangeFlags(ref bool flag1,ref bool flag2, ref bool flag3)
    {
        Array.Fill(flags, false);
        flag1 = true;
        flag2 = true;
        flag3 = true;
    }
    


    void Execute()
    {
        
        if (fetchBuffer.Length < 6)
        {
            Console.WriteLine($"ERROR: fetchBuffer length is {fetchBuffer.Length}, expected 6!");
            return;
        }

        var op = (OpCode)fetchBuffer[0];
        var opand1 = fetchBuffer[1];
        var opand2 = fetchBuffer[2];
        var opand3 = fetchBuffer[3];
        
        dataBuffer[0] = fetchBuffer[2];
        dataBuffer[1] = fetchBuffer[3];
        dataBuffer[2] = fetchBuffer[4];
        dataBuffer[3] = fetchBuffer[5];
        
        ref var data = ref dataBuffer;
        
        

        switch (op)
        {
            case OpCode.ADDL:
                if (!IsValidRegister(opand1) || !IsValidRegister(opand2) || !IsValidRegister(opand3)) return;
                SetReg(opand1, GetReg(opand2) + GetReg(opand3));
                pcUpdate = 4;
                break;

            case OpCode.SUBL:
                if (!IsValidRegister(opand1) || !IsValidRegister(opand2) || !IsValidRegister(opand3)) return;
                SetReg(opand1, GetReg(opand2) - GetReg(opand3));
                pcUpdate = 4;
                break;

            case OpCode.MULL:
                if (!IsValidRegister(opand1) || !IsValidRegister(opand2) || !IsValidRegister(opand3)) return;
                SetReg(opand1, GetReg(opand2) * GetReg(opand3));
                pcUpdate = 4;
                break;

            case OpCode.DIVL:
                if (!IsValidRegister(opand1) || !IsValidRegister(opand2) || !IsValidRegister(opand3)) return;
                if (GetReg(opand3) == 0) return;
                SetReg(opand1, GetReg(opand2) / GetReg(opand3));
                pcUpdate = 4;
                break;

            case OpCode.MODL:
                if (!IsValidRegister(opand1) || !IsValidRegister(opand2) || !IsValidRegister(opand3)) return;
                if (GetReg(opand3) == 0) return;
                SetReg(opand1, GetReg(opand2) % GetReg(opand3));
                pcUpdate = 4;
                break;

            case OpCode.ANDL:
                if (!IsValidRegister(opand1) || !IsValidRegister(opand2) || !IsValidRegister(opand3)) return;
                SetReg(opand1, GetReg(opand2) & GetReg(opand3));
                pcUpdate = 4;
                break;

            case OpCode.ORL:
                if (!IsValidRegister(opand1) || !IsValidRegister(opand2) || !IsValidRegister(opand3)) return;
                SetReg(opand1, GetReg(opand2) | GetReg(opand3));
                pcUpdate = 4;
                break;

            case OpCode.XORL:
                if (!IsValidRegister(opand1) || !IsValidRegister(opand2) || !IsValidRegister(opand3)) return;
                SetReg(opand1, GetReg(opand2) ^ GetReg(opand3));
                pcUpdate = 4;
                break;

            case OpCode.NOTL:
                if (!IsValidRegister(opand1) || !IsValidRegister(opand2)) return;
                SetReg(opand1, ~GetReg(opand2));
                pcUpdate = 3;
                break;

            case OpCode.SHLL:
                if (!IsValidRegister(opand1) || !IsValidRegister(opand2) || !IsValidRegister(opand3)) return;
                SetReg(opand1, GetReg(opand2) << (int)GetReg(opand3));
                pcUpdate = 4;
                break;

            case OpCode.SHRL:
                if (!IsValidRegister(opand1) || !IsValidRegister(opand2) || !IsValidRegister(opand3)) return;
                SetReg(opand1, GetReg(opand2) >> (int)GetReg(opand3));
                pcUpdate = 4;
                break;

            case OpCode.ADD:
                if (!IsValidRegister(opand1) || !IsValidRegister(opand2) || !IsValidRegister(opand3)) return;
                WriteByteToRegister(opand1,  ReadByteFromRegister(opand2) + ReadByteFromRegister(opand3));
                pcUpdate = 4;
                break;

            case OpCode.SUB:
                if (!IsValidRegister(opand1) || !IsValidRegister(opand2) || !IsValidRegister(opand3)) return;
                WriteByteToRegister(opand1, ReadByteFromRegister(opand2) - ReadByteFromRegister(opand3));
                pcUpdate = 4;
                break;

            case OpCode.MUL:
                if (!IsValidRegister(opand1) || !IsValidRegister(opand2) || !IsValidRegister(opand3)) return;
                WriteByteToRegister(opand1, ReadByteFromRegister(opand2) * ReadByteFromRegister(opand3));
                pcUpdate = 4;
                break;

            case OpCode.DIV:
                if (!IsValidRegister(opand1) || !IsValidRegister(opand2) || !IsValidRegister(opand3)) return;
                if (ReadByteFromRegister(opand3) == 0) return;
                WriteByteToRegister(opand1, ReadByteFromRegister(opand2) / ReadByteFromRegister(opand3));
                pcUpdate = 4;
                break;

            case OpCode.MOD:
                if (!IsValidRegister(opand1) || !IsValidRegister(opand2) || !IsValidRegister(opand3)) return;
                if (ReadByteFromRegister(opand3) == 0) return;
                WriteByteToRegister(opand1, ReadByteFromRegister(opand2) % ReadByteFromRegister(opand3));
                pcUpdate = 4;
                break;

            case OpCode.AND:
                if (!IsValidRegister(opand1) || !IsValidRegister(opand2) || !IsValidRegister(opand3)) return;
                WriteByteToRegister(opand1, ReadByteFromRegister(opand2) & ReadByteFromRegister(opand3));
                pcUpdate = 4;
                break;

            case OpCode.OR:
                if (!IsValidRegister(opand1) || !IsValidRegister(opand2) || !IsValidRegister(opand3)) return;
                WriteByteToRegister(opand1, ReadByteFromRegister(opand2) | ReadByteFromRegister(opand3));
                pcUpdate = 4;
                break;

            case OpCode.XOR:
                if (!IsValidRegister(opand1) || !IsValidRegister(opand2) || !IsValidRegister(opand3)) return;
                WriteByteToRegister(opand1, ReadByteFromRegister(opand2) ^ ReadByteFromRegister(opand3));
                pcUpdate = 4;
                break;

            case OpCode.NOT:
                if (!IsValidRegister(opand1) || !IsValidRegister(opand2)) return;
                WriteByteToRegister(opand1, ~ReadByteFromRegister(opand2));
                pcUpdate = 3;
                break;

            case OpCode.SHL:
                if (!IsValidRegister(opand1) || !IsValidRegister(opand2) || !IsValidRegister(opand3)) return;
                WriteByteToRegister(opand1, ReadByteFromRegister(opand2) << ReadByteFromRegister(opand3));
                pcUpdate = 4;
                break;

            case OpCode.SHR:
                if (!IsValidRegister(opand1) || !IsValidRegister(opand2) || !IsValidRegister(opand3)) return;
                WriteByteToRegister(opand1, ReadByteFromRegister(opand2) >> ReadByteFromRegister(opand3));
                pcUpdate = 4;
                break;

            case OpCode.CMP:
                pcUpdate = 3;
                if (!IsValidRegister(opand1) || !IsValidRegister(opand2)) return;
                if (GetReg(opand1) == GetReg(opand2))
                {
                    ChangeFlags(ref flags[0], ref flags[5], ref flags[4]);
                }
                else if (GetReg(opand1) < GetReg(opand2))
                {
                    ChangeFlags(ref flags[3], ref flags[4], ref flags[1]);
                }
                else
                {
                    ChangeFlags(ref flags[4], ref flags[5], ref flags[1]);
                }

                break;

            case OpCode.JMP:
                pcUpdate = 0;
                if (!IsValidRegister(opand1)) return;
                PC = GetReg(opand1);
                break;

            case OpCode.JEQ:
                pcUpdate = 2;
                if (!IsValidRegister(opand1)) return;
                if (flags[0])
                {
                    PC = GetReg(opand1);
                    pcUpdate = 0;
                }

                break;

            case OpCode.JNE:
                pcUpdate = 2;
                if (!IsValidRegister(opand1)) return;
                if (flags[1])
                {
                    PC = GetReg(opand1);
                    pcUpdate = 0;
                }

                break;

            case OpCode.JGT:
                pcUpdate = 2;
                if (!IsValidRegister(opand1)) return;
                if (flags[4])
                {
                    PC = GetReg(opand1);
                    pcUpdate = 0;
                }

                break;

            case OpCode.JLT:
                pcUpdate = 2;
                if (!IsValidRegister(opand1)) return;
                if (flags[3])
                {
                    PC = GetReg(opand1);
                    pcUpdate = 0;
                }

                break;

            case OpCode.JGE:
                pcUpdate = 2;
                if (!IsValidRegister(opand1)) return;
                if (flags[5])
                {
                    PC = GetReg(opand1);
                    pcUpdate = 0;
                }

                break;

            case OpCode.JLE:
                pcUpdate = 2;
                if (!IsValidRegister(opand1)) return;
                if (flags[4])
                {
                    PC = GetReg(opand1);
                    pcUpdate = 0;
                }
                
                

                break;

            case OpCode.CALL:
                pcUpdate = 0;
                registers.SP -= 4;
                memoryBus.WriteWord(registers.SP, PC + 2);
                PC = GetReg(opand1);
                break;

            case OpCode.RET:
                pcUpdate = 0;
                PC = memoryBus.ReadWord(registers.SP);
                registers.SP += 4;
                break;

            case OpCode.PUSH:
                pcUpdate = 2;
                if (!IsValidRegister(opand1)) return;
                registers.SP -= 4;
                memoryBus.WriteWord(registers.SP, GetReg(opand1));
                break;

            case OpCode.POP:
                pcUpdate = 2;
                if (!IsValidRegister(opand1)) return;
                SetReg(opand1, memoryBus.ReadWord(registers.SP));
                registers.SP += 4;
                break;

            case OpCode.STORE:
                pcUpdate = 3;
                if (!IsValidRegister(opand2)) return;
                memoryBus.WriteByte(GetReg(opand1), ReadByteFromRegister(opand2));
                break;

            case OpCode.LOAD:
                pcUpdate = 3;
                if (!IsValidRegister(opand1) || !IsValidRegister(opand2)) return;
                WriteByteToRegister(opand1, memoryBus.ReadByte(GetReg(opand2)));
                break;

            case OpCode.MOV:
                pcUpdate = 3;
                if (!IsValidRegister(opand1)) return;
                WriteByteToRegister(opand1, opand2);
                break;

            case OpCode.STORE_L:
                pcUpdate = 3;
                if (!IsValidRegister(opand1) || !IsValidRegister(opand2)) return;
                memoryBus.WriteWord(GetReg(opand1), GetReg(opand2));
                break;

            case OpCode.LOAD_L:
                pcUpdate = 3;
                if (!IsValidRegister(opand1) || !IsValidRegister(opand2)) return;
                SetReg(opand1, memoryBus.ReadWord(GetReg(opand2)));
                break;

            case OpCode.MOV_L:
                pcUpdate = 6;
                if (!IsValidRegister(opand1)) return;
                SetReg(opand1, (uint)BitConverter.ToInt32(data, 0));
                break;

            case OpCode.NOP:
                pcUpdate = 1;
                break;

            case OpCode.HALT:
                pcUpdate = 1;
                halted = true;
                break;

            case OpCode.INC:
                pcUpdate = 2;
                if (!IsValidRegister(opand1)) return;
                SetReg(opand1, GetReg(opand1) + 1);
                break;

            case OpCode.DEC:
                pcUpdate = 2;
                if (!IsValidRegister(opand1)) return;
                SetReg(opand1, GetReg(opand1) - 1);
                break;

            default:
                pcUpdate = 1;
                Fault.FaultCpu(CpuTrapCause.IllegalInstruction, (int) op);
                break;
        }
    }


    void HandleTrap()
    {
        faultPending = false;
        
        controlStatusRegisters.epc = controlStatusRegisters.epc with { value = (int)PC };

        controlStatusRegisters.cause = controlStatusRegisters.cause with { value = (int)Fault.cause };

        controlStatusRegisters.tval = controlStatusRegisters.tval with { value = Fault.info };

        privilege = privilege switch
        {
            CpuPrivelege.User => CpuPrivelege.Supervisor,
            _ => CpuPrivelege.Machine
        };

        PC = (uint)controlStatusRegisters.tvec.value;
    }
}