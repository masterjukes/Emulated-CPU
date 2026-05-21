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
    
    
    byte[] fetchBuffer = new byte[4];


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
            Execute();
            PC += 4;
        }
        catch (CpuFault fault)
        {
            HandleTrap(fault);
        }

        controlStatusRegisters.cycle =
            controlStatusRegisters.cycle with { value = controlStatusRegisters.cycle.value + 1 };
        
    }

    void Fetch()
    {
        fetchBuffer[0] = memoryBus.ReadByte(PC, true);
        fetchBuffer[1] = memoryBus.ReadByte(PC + 1, true);
        fetchBuffer[2] = memoryBus.ReadByte(PC + 2, true);
        fetchBuffer[3] = memoryBus.ReadByte(PC + 3, true);
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]

    bool IsValidRegister(byte reg)
    {
        if (reg > 31)
            throw new CpuFault(CpuTrapCause.IllegalInstruction, 0);
        return true;
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]

    void set_reg(byte reg, uint value)
    {
        registers[reg] = (int) value;
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    uint get_reg(byte reg)
    {
        return (uint) registers[reg];
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    void writeBytetoRegister(byte reg, int value)
    {
        registers[reg] = value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    byte readBytefromRegister(byte reg)
    {
        return (byte) registers[reg];
    }



    void changeflags(ref bool flag1,ref bool flag2, ref bool flag3)
    {
        flags.All(x => x = false);
        flag1 = true;
        flag2 = true;
        flag3 = true;
    }
    


    void Execute()
    {
        var instruction = fetchBuffer;
        var op = (OpCode)instruction[0];
        var opand1 = instruction[1];
        var opand2 = instruction[2];
        var opand3 = instruction[3];
        var data = instruction; // [ 0, 1, 2, 3]
        data[0] = 0;


        switch (op)
        {
            case OpCode.ADDL:
                if (!IsValidRegister(opand1) || !IsValidRegister(opand2) || !IsValidRegister(opand3))
                    throw new CpuFault(CpuTrapCause.IllegalInstruction, 0);
                break;

            case OpCode.SUBL:
                if (!IsValidRegister(opand1) || !IsValidRegister(opand2) || !IsValidRegister(opand3)) return;
                set_reg(opand1, get_reg(opand2) - get_reg(opand3));
                break;

            case OpCode.MULL:
                if (!IsValidRegister(opand1) || !IsValidRegister(opand2) || !IsValidRegister(opand3)) return;
                set_reg(opand1, get_reg(opand2) * get_reg(opand3));
                break;

            case OpCode.DIVL:
                if (!IsValidRegister(opand1) || !IsValidRegister(opand2) || !IsValidRegister(opand3)) return;
                if (get_reg(opand3) == 0) return;
                set_reg(opand1, get_reg(opand2) / get_reg(opand3));
                break;

            case OpCode.MODL:
                if (!IsValidRegister(opand1) || !IsValidRegister(opand2) || !IsValidRegister(opand3)) return;
                if (get_reg(opand3) == 0) return;
                set_reg(opand1, get_reg(opand2) % get_reg(opand3));
                break;

            case OpCode.ANDL:
                if (!IsValidRegister(opand1) || !IsValidRegister(opand2) || !IsValidRegister(opand3)) return;
                set_reg(opand1, get_reg(opand2) & get_reg(opand3));
                break;

            case OpCode.ORL:
                if (!IsValidRegister(opand1) || !IsValidRegister(opand2) || !IsValidRegister(opand3)) return;
                set_reg(opand1, get_reg(opand2) | get_reg(opand3));
                break;

            case OpCode.XORL:
                if (!IsValidRegister(opand1) || !IsValidRegister(opand2) || !IsValidRegister(opand3)) return;
                set_reg(opand1, get_reg(opand2) ^ get_reg(opand3));
                break;

            case OpCode.NOTL:
                if (!IsValidRegister(opand1) || !IsValidRegister(opand2)) return;
                set_reg(opand1, ~get_reg(opand2));
                break;

            case OpCode.SHLL:
                if (!IsValidRegister(opand1) || !IsValidRegister(opand2) || !IsValidRegister(opand3)) return;
                set_reg(opand1, get_reg(opand2) << (int)get_reg(opand3));
                break;

            case OpCode.SHRL:
                if (!IsValidRegister(opand1) || !IsValidRegister(opand2) || !IsValidRegister(opand3)) return;
                set_reg(opand1, get_reg(opand2) >> (int)get_reg(opand3));
                break;

            case OpCode.ADD:
                if (!IsValidRegister(opand1) || !IsValidRegister(opand2) || !IsValidRegister(opand3)) return;
                writeBytetoRegister(opand1,  readBytefromRegister(opand2) + readBytefromRegister(opand3));
                break;

            case OpCode.SUB:
                if (!IsValidRegister(opand1) || !IsValidRegister(opand2) || !IsValidRegister(opand3)) return;
                writeBytetoRegister(opand1, readBytefromRegister(opand2) - readBytefromRegister(opand3));
                break;

            case OpCode.MUL:
                if (!IsValidRegister(opand1) || !IsValidRegister(opand2) || !IsValidRegister(opand3)) return;
                writeBytetoRegister(opand1, readBytefromRegister(opand2) * readBytefromRegister(opand3));
                break;

            case OpCode.DIV:
                if (!IsValidRegister(opand1) || !IsValidRegister(opand2) || !IsValidRegister(opand3)) return;
                if (readBytefromRegister(opand3) == 0) return;
                writeBytetoRegister(opand1, readBytefromRegister(opand2) / readBytefromRegister(opand3));
                break;

            case OpCode.MOD:
                if (!IsValidRegister(opand1) || !IsValidRegister(opand2) || !IsValidRegister(opand3)) return;
                if (readBytefromRegister(opand3) == 0) return;
                writeBytetoRegister(opand1, readBytefromRegister(opand2) % readBytefromRegister(opand3));
                break;

            case OpCode.AND:
                if (!IsValidRegister(opand1) || !IsValidRegister(opand2) || !IsValidRegister(opand3)) return;
                writeBytetoRegister(opand1, readBytefromRegister(opand2) & readBytefromRegister(opand3));
                break;

            case OpCode.OR:
                if (!IsValidRegister(opand1) || !IsValidRegister(opand2) || !IsValidRegister(opand3)) return;
                writeBytetoRegister(opand1, readBytefromRegister(opand2) | readBytefromRegister(opand3));
                break;

            case OpCode.XOR:
                if (!IsValidRegister(opand1) || !IsValidRegister(opand2) || !IsValidRegister(opand3)) return;
                writeBytetoRegister(opand1, readBytefromRegister(opand2) ^ readBytefromRegister(opand3));
                break;

            case OpCode.NOT:
                if (!IsValidRegister(opand1) || !IsValidRegister(opand2)) return;
                writeBytetoRegister(opand1, ~readBytefromRegister(opand2));
                break;

            case OpCode.SHL:
                if (!IsValidRegister(opand1) || !IsValidRegister(opand2) || !IsValidRegister(opand3)) return;
                writeBytetoRegister(opand1, readBytefromRegister(opand2) << readBytefromRegister(opand3));
                break;

            case OpCode.SHR:
                if (!IsValidRegister(opand1) || !IsValidRegister(opand2) || !IsValidRegister(opand3)) return;
                writeBytetoRegister(opand1, readBytefromRegister(opand2) >> readBytefromRegister(opand3));
                break;

            case OpCode.CMP:
                if (!IsValidRegister(opand1) || !IsValidRegister(opand2)) return;
                if (get_reg(opand1) == get_reg(opand2))
                {
                    changeflags(ref flags[0], ref flags[5], ref flags[4]);
                }
                else if (get_reg(opand1) < get_reg(opand2))
                {
                    changeflags(ref flags[3], ref flags[4], ref flags[1]);
                }
                else
                {
                    changeflags(ref flags[4], ref flags[5], ref flags[1]);
                }

                break;

            case OpCode.JMP:
                if (!IsValidRegister(opand1)) return;
                PC = get_reg(opand1);
                break;

            case OpCode.JEQ:
                if (!IsValidRegister(opand1)) return;
                if (flags[0])
                {
                    PC = get_reg(opand1);
                }

                break;

            case OpCode.JNE:
                if (!IsValidRegister(opand1)) return;
                if (flags[1])
                {
                    PC = get_reg(opand1);
                }

                break;

            case OpCode.JGT:
                if (!IsValidRegister(opand1)) return;
                if (flags[4])
                {
                    PC = get_reg(opand1);
                }

                break;

            case OpCode.JLT:
                if (!IsValidRegister(opand1)) return;
                if (flags[3])
                {
                    PC = get_reg(opand1);
                }

                break;

            case OpCode.JGE:
                if (!IsValidRegister(opand1)) return;
                if (flags[5])
                {
                    PC = get_reg(opand1);
                }

                break;

            case OpCode.JLE:
                if (!IsValidRegister(opand1)) return;
                if (flags[4])
                {
                    PC = get_reg(opand1);
                }
                
                

                break;

            case OpCode.CALL:
                registers.SP -= 4;
                memoryBus.WriteWord(registers.SP, PC + 2);
                PC = get_reg(opand1);
                break;

            case OpCode.RET:
                PC = memoryBus.ReadWord(registers.SP);
                registers.SP += 4;
                break;

            case OpCode.PUSH:
                if (!IsValidRegister(opand1)) return;
                registers.SP -= 4;
                memoryBus.WriteWord(registers.SP, get_reg(opand1));
                break;

            case OpCode.POP:
                if (!IsValidRegister(opand1)) return;
                set_reg(opand1, memoryBus.ReadWord(registers.SP));
                registers.SP += 4;
                break;

            case OpCode.STORE:
                if (!IsValidRegister(opand2)) return;
                memoryBus.WriteByte(get_reg(opand1), readBytefromRegister(opand2));
                break;

            case OpCode.LOAD:
                if (!IsValidRegister(opand1) || !IsValidRegister(opand2)) return;
                writeBytetoRegister(opand1, memoryBus.ReadByte(get_reg(opand2)));
                break;

            case OpCode.MOV:
                if (!IsValidRegister(opand1)) return;
                writeBytetoRegister(opand1, opand2);
                break;

            case OpCode.STORE_L:
                ;
                if (!IsValidRegister(opand1) || !IsValidRegister(opand2)) return;
                memoryBus.WriteWord(get_reg(opand1), get_reg(opand2));
                break;

            case OpCode.LOAD_L:
                if (!IsValidRegister(opand1) || !IsValidRegister(opand2)) return;
                set_reg(opand1, memoryBus.ReadWord(get_reg(opand2)));
                break;

            case OpCode.MOV_L:
                if (!IsValidRegister(opand1)) return;
                set_reg(opand1, (uint)BitConverter.ToInt32(data, 0));
                break;

            case OpCode.NOP:
                break;

            case OpCode.HALT:
                halted = true;
                break;

            case OpCode.INC:
                if (!IsValidRegister(opand1)) return;
                set_reg(opand1, get_reg(opand1) + 1);
                break;

            case OpCode.DEC:
                if (!IsValidRegister(opand1)) return;
                set_reg(opand1, get_reg(opand1) - 1);
                break;

            default:
                throw new CpuFault(CpuTrapCause.IllegalInstruction, 0);
        }
    }


    void HandleTrap(CpuFault fault)
    {
        controlStatusRegisters.epc = controlStatusRegisters.epc with { value = (int)PC };

        controlStatusRegisters.cause = controlStatusRegisters.cause with { value = (int)fault.cause };

        controlStatusRegisters.tval = controlStatusRegisters.tval with { value = fault.info };

        privilege = privilege switch
        {
            CpuPrivelege.User => CpuPrivelege.Supervisor,
            _ => CpuPrivelege.Machine
        };

        PC = (uint)controlStatusRegisters.tvec.value;
    }
}