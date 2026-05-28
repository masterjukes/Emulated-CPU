using System.Runtime.InteropServices;
using SDL2;

namespace Fun_CPU.Vga;

public class PS2Keyboard : MMIODevice
{
    private class KeyState
    {
        public bool down;
        public float timer;
    }

    readonly KeyState[] keys = Enumerable
        .Range(0, (int)SDL.SDL_Scancode.SDL_NUM_SCANCODES)
        .Select(_ => new KeyState())
        .ToArray();

    readonly Queue<byte> fifo = new();

    public override int size => 0x10;
    public override float updateDeltaTime => 2;

    const int REG_DATA = 0;
    const int REG_STATUS = 1;
    const int REG_COMMAND = 2;

    const byte STATUS_OUTPUT_FULL = 1 << 0;
    const byte STATUS_IRQ_PENDING = 1 << 1;

    readonly float repeatDelay = 0.5f;
    readonly float repeatRate = 0.05f;

    static readonly Dictionary<SDL.SDL_Scancode, byte> ScanCodes = new()
    {
        { SDL.SDL_Scancode.SDL_SCANCODE_A, 0x1E },
        { SDL.SDL_Scancode.SDL_SCANCODE_B, 0x30 },
        { SDL.SDL_Scancode.SDL_SCANCODE_C, 0x2E },
        { SDL.SDL_Scancode.SDL_SCANCODE_D, 0x20 },
        { SDL.SDL_Scancode.SDL_SCANCODE_E, 0x12 },
        { SDL.SDL_Scancode.SDL_SCANCODE_F, 0x21 },
        { SDL.SDL_Scancode.SDL_SCANCODE_G, 0x22 },
        { SDL.SDL_Scancode.SDL_SCANCODE_H, 0x23 },
        { SDL.SDL_Scancode.SDL_SCANCODE_I, 0x17 },
        { SDL.SDL_Scancode.SDL_SCANCODE_J, 0x24 },
        { SDL.SDL_Scancode.SDL_SCANCODE_K, 0x25 },
        { SDL.SDL_Scancode.SDL_SCANCODE_L, 0x26 },
        { SDL.SDL_Scancode.SDL_SCANCODE_M, 0x32 },
        { SDL.SDL_Scancode.SDL_SCANCODE_N, 0x31 },
        { SDL.SDL_Scancode.SDL_SCANCODE_O, 0x18 },
        { SDL.SDL_Scancode.SDL_SCANCODE_P, 0x19 },
        { SDL.SDL_Scancode.SDL_SCANCODE_Q, 0x10 },
        { SDL.SDL_Scancode.SDL_SCANCODE_R, 0x13 },
        { SDL.SDL_Scancode.SDL_SCANCODE_S, 0x1F },
        { SDL.SDL_Scancode.SDL_SCANCODE_T, 0x14 },
        { SDL.SDL_Scancode.SDL_SCANCODE_U, 0x16 },
        { SDL.SDL_Scancode.SDL_SCANCODE_V, 0x2F },
        { SDL.SDL_Scancode.SDL_SCANCODE_W, 0x11 },
        { SDL.SDL_Scancode.SDL_SCANCODE_X, 0x2D },
        { SDL.SDL_Scancode.SDL_SCANCODE_Y, 0x15 },
        { SDL.SDL_Scancode.SDL_SCANCODE_Z, 0x2C },

        { SDL.SDL_Scancode.SDL_SCANCODE_0, 0x0B },
        { SDL.SDL_Scancode.SDL_SCANCODE_1, 0x02 },
        { SDL.SDL_Scancode.SDL_SCANCODE_2, 0x03 },
        { SDL.SDL_Scancode.SDL_SCANCODE_3, 0x04 },
        { SDL.SDL_Scancode.SDL_SCANCODE_4, 0x05 },
        { SDL.SDL_Scancode.SDL_SCANCODE_5, 0x06 },
        { SDL.SDL_Scancode.SDL_SCANCODE_6, 0x07 },
        { SDL.SDL_Scancode.SDL_SCANCODE_7, 0x08 },
        { SDL.SDL_Scancode.SDL_SCANCODE_8, 0x09 },
        { SDL.SDL_Scancode.SDL_SCANCODE_9, 0x0A },

        { SDL.SDL_Scancode.SDL_SCANCODE_RETURN, 0x1C },
        { SDL.SDL_Scancode.SDL_SCANCODE_ESCAPE, 0x01 },
        { SDL.SDL_Scancode.SDL_SCANCODE_BACKSPACE, 0x0E },
        { SDL.SDL_Scancode.SDL_SCANCODE_TAB, 0x0F },
        { SDL.SDL_Scancode.SDL_SCANCODE_SPACE, 0x39 },

        { SDL.SDL_Scancode.SDL_SCANCODE_LSHIFT, 0x2A },
        { SDL.SDL_Scancode.SDL_SCANCODE_RSHIFT, 0x2A },
        { SDL.SDL_Scancode.SDL_SCANCODE_LCTRL, 0x1D },
        { SDL.SDL_Scancode.SDL_SCANCODE_RCTRL, 0x1D },
        { SDL.SDL_Scancode.SDL_SCANCODE_LALT, 0x38 },
        { SDL.SDL_Scancode.SDL_SCANCODE_RALT, 0x38 },

        { SDL.SDL_Scancode.SDL_SCANCODE_F1, 0x3B },
        { SDL.SDL_Scancode.SDL_SCANCODE_F2, 0x3C },
        { SDL.SDL_Scancode.SDL_SCANCODE_F3, 0x3D },
        { SDL.SDL_Scancode.SDL_SCANCODE_F4, 0x3E },
        { SDL.SDL_Scancode.SDL_SCANCODE_F5, 0x3F },
        { SDL.SDL_Scancode.SDL_SCANCODE_F6, 0x40 },
        { SDL.SDL_Scancode.SDL_SCANCODE_F7, 0x41 },
        { SDL.SDL_Scancode.SDL_SCANCODE_F8, 0x42 },
        { SDL.SDL_Scancode.SDL_SCANCODE_F9, 0x43 },
        { SDL.SDL_Scancode.SDL_SCANCODE_F10, 0x44 },
        { SDL.SDL_Scancode.SDL_SCANCODE_F11, 0x57 },
        { SDL.SDL_Scancode.SDL_SCANCODE_F12, 0x58 },

        { SDL.SDL_Scancode.SDL_SCANCODE_LEFT, 0x4B },
        { SDL.SDL_Scancode.SDL_SCANCODE_UP, 0x48 },
        { SDL.SDL_Scancode.SDL_SCANCODE_RIGHT, 0x4D },
        { SDL.SDL_Scancode.SDL_SCANCODE_DOWN, 0x50 },

        { SDL.SDL_Scancode.SDL_SCANCODE_HOME, 0x47 },
        { SDL.SDL_Scancode.SDL_SCANCODE_END, 0x4F },
        { SDL.SDL_Scancode.SDL_SCANCODE_PAGEUP, 0x49 },
        { SDL.SDL_Scancode.SDL_SCANCODE_PAGEDOWN, 0x51 },
        { SDL.SDL_Scancode.SDL_SCANCODE_INSERT, 0x52 },
        { SDL.SDL_Scancode.SDL_SCANCODE_DELETE, 0x53 },

        { SDL.SDL_Scancode.SDL_SCANCODE_KP_0, 0x52 },
        { SDL.SDL_Scancode.SDL_SCANCODE_KP_1, 0x4F },
        { SDL.SDL_Scancode.SDL_SCANCODE_KP_2, 0x50 },
        { SDL.SDL_Scancode.SDL_SCANCODE_KP_3, 0x51 },
        { SDL.SDL_Scancode.SDL_SCANCODE_KP_4, 0x4B },
        { SDL.SDL_Scancode.SDL_SCANCODE_KP_5, 0x4C },
        { SDL.SDL_Scancode.SDL_SCANCODE_KP_6, 0x4D },
        { SDL.SDL_Scancode.SDL_SCANCODE_KP_7, 0x47 },
        { SDL.SDL_Scancode.SDL_SCANCODE_KP_8, 0x48 },
        { SDL.SDL_Scancode.SDL_SCANCODE_KP_9, 0x49 },

        { SDL.SDL_Scancode.SDL_SCANCODE_KP_MULTIPLY, 0x37 },
        { SDL.SDL_Scancode.SDL_SCANCODE_KP_PLUS, 0x4E },
        { SDL.SDL_Scancode.SDL_SCANCODE_KP_MINUS, 0x4A },
        { SDL.SDL_Scancode.SDL_SCANCODE_KP_PERIOD, 0x53 },
        { SDL.SDL_Scancode.SDL_SCANCODE_KP_DIVIDE, 0x35 },
    };

    public override void UpdateDevice()
    {
        PollKeyboard();
        HandleGuestCommands();
        UpdateRegisters();
    }

    void PollKeyboard()
    {
        SDL.SDL_PumpEvents();
        nint state = SDL.SDL_GetKeyboardState(out _);

        foreach (var (scancode, sc) in ScanCodes)
        {
            int index = (int)scancode;
            bool down = Marshal.ReadByte(state, index) != 0;

            if (down)
            {
                if (!keys[index].down)
                {
                    KeyDown(scancode, sc);
                    keys[index].timer = repeatDelay;
                }
                else
                {
                    keys[index].timer -= updateDeltaTime / 1000f;

                    if (keys[index].timer <= 0)
                    {
                        KeyDown(scancode, sc);
                        keys[index].timer = repeatRate;
                    }
                }
            }
            else if (keys[index].down)
            {
                KeyUp(scancode, sc);
            }

            keys[index].down = down;
        }
    }

    void KeyDown(SDL.SDL_Scancode scancode, byte sc)
    {
        if (scancode == SDL.SDL_Scancode.SDL_SCANCODE_CAPSLOCK)
        {
            PushScancode(0x3A);
            PushScancode(0xBA);
            return;
        }

        PushScancode(sc);
    }

    void KeyUp(SDL.SDL_Scancode scancode, byte sc)
    {
        if (scancode == SDL.SDL_Scancode.SDL_SCANCODE_CAPSLOCK)
            return;

        PushScancode((byte)(sc | 0x80));
    }

    void PushScancode(byte code)
    {
        fifo.Enqueue(code);

        SetStatusBit(STATUS_OUTPUT_FULL);
        SetStatusBit(STATUS_IRQ_PENDING);

        Cpu.instance.controlStatusRegisters.ip.value = (int)InterruptCause.External;
        Cpu.instance.irqPending |= Cpu.IRQ_KEYBOARD;
        Fault.FaultCpu(CpuTrapCause.Interrupt, baseAddress);
    }

    void HandleGuestCommands()
    {
        byte cmd = Cpu.instance.memoryBus.dev[baseAddress + REG_COMMAND];

        if (cmd == 0)
            return;

        switch (cmd)
        {
            case 0xFF:
                fifo.Enqueue(0xFA);
                fifo.Enqueue(0xAA);
                break;

            case 0xF4:
                fifo.Enqueue(0xFA);
                break;
        }

        Cpu.instance.memoryBus.dev[baseAddress + REG_COMMAND] = 0;
    }

    void UpdateRegisters()
    {
        if (fifo.Count > 0)
            Cpu.instance.memoryBus.dev[baseAddress + REG_DATA] = fifo.Peek();
        else
        {
            Cpu.instance.memoryBus.dev[baseAddress + REG_DATA] = 0;
            ClearStatusBit(STATUS_OUTPUT_FULL);
            ClearStatusBit(STATUS_IRQ_PENDING);
        }
    }

    public override void ReadByte(uint address)
    {
        var offset = address - (uint)baseAddress;

        if (offset != REG_DATA)
            return;

        if (fifo.Count == 0)
            return;

        fifo.Dequeue();

        if (fifo.Count == 0)
        {
            ClearStatusBit(STATUS_OUTPUT_FULL);
            ClearStatusBit(STATUS_IRQ_PENDING);
        }
    }

    void SetStatusBit(byte bit)
    {
        byte s = Cpu.instance.memoryBus.dev[baseAddress + REG_STATUS];
        Cpu.instance.memoryBus.dev[baseAddress + REG_STATUS] = (byte)(s | bit);
    }

    void ClearStatusBit(byte bit)
    {
        byte s = Cpu.instance.memoryBus.dev[baseAddress + REG_STATUS];
        Cpu.instance.memoryBus.dev[baseAddress + REG_STATUS] = (byte)(s & ~bit);
    }
}
