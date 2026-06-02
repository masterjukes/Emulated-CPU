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
    const int REG_DATA_EXTENDED = 1;
    const int REG_STATUS = 2;
    const int REG_COMMAND = 3;

    const byte STATUS_OUTPUT_FULL = 1 << 0;
    const byte STATUS_IRQ_PENDING = 1 << 1;

    readonly float repeatDelay = 0.5f;
    readonly float repeatRate = 0.05f;

static readonly Dictionary<SDL.SDL_Scancode, byte[]> ScanCodes = new()
{
    // Letters
    { SDL.SDL_Scancode.SDL_SCANCODE_A, new byte[]{0x1E} },
    { SDL.SDL_Scancode.SDL_SCANCODE_B, new byte[]{0x30} },
    { SDL.SDL_Scancode.SDL_SCANCODE_C, new byte[]{0x2E} },
    { SDL.SDL_Scancode.SDL_SCANCODE_D, new byte[]{0x20} },
    { SDL.SDL_Scancode.SDL_SCANCODE_E, new byte[]{0x12} },
    { SDL.SDL_Scancode.SDL_SCANCODE_F, new byte[]{0x21} },
    { SDL.SDL_Scancode.SDL_SCANCODE_G, new byte[]{0x22} },
    { SDL.SDL_Scancode.SDL_SCANCODE_H, new byte[]{0x23} },
    { SDL.SDL_Scancode.SDL_SCANCODE_I, new byte[]{0x17} },
    { SDL.SDL_Scancode.SDL_SCANCODE_J, new byte[]{0x24} },
    { SDL.SDL_Scancode.SDL_SCANCODE_K, new byte[]{0x25} },
    { SDL.SDL_Scancode.SDL_SCANCODE_L, new byte[]{0x26} },
    { SDL.SDL_Scancode.SDL_SCANCODE_M, new byte[]{0x32} },
    { SDL.SDL_Scancode.SDL_SCANCODE_N, new byte[]{0x31} },
    { SDL.SDL_Scancode.SDL_SCANCODE_O, new byte[]{0x18} },
    { SDL.SDL_Scancode.SDL_SCANCODE_P, new byte[]{0x19} },
    { SDL.SDL_Scancode.SDL_SCANCODE_Q, new byte[]{0x10} },
    { SDL.SDL_Scancode.SDL_SCANCODE_R, new byte[]{0x13} },
    { SDL.SDL_Scancode.SDL_SCANCODE_S, new byte[]{0x1F} },
    { SDL.SDL_Scancode.SDL_SCANCODE_T, new byte[]{0x14} },
    { SDL.SDL_Scancode.SDL_SCANCODE_U, new byte[]{0x16} },
    { SDL.SDL_Scancode.SDL_SCANCODE_V, new byte[]{0x2F} },
    { SDL.SDL_Scancode.SDL_SCANCODE_W, new byte[]{0x11} },
    { SDL.SDL_Scancode.SDL_SCANCODE_X, new byte[]{0x2D} },
    { SDL.SDL_Scancode.SDL_SCANCODE_Y, new byte[]{0x15} },
    { SDL.SDL_Scancode.SDL_SCANCODE_Z, new byte[]{0x2C} },

    // Numbers
    { SDL.SDL_Scancode.SDL_SCANCODE_1, new byte[]{0x02} },
    { SDL.SDL_Scancode.SDL_SCANCODE_2, new byte[]{0x03} },
    { SDL.SDL_Scancode.SDL_SCANCODE_3, new byte[]{0x04} },
    { SDL.SDL_Scancode.SDL_SCANCODE_4, new byte[]{0x05} },
    { SDL.SDL_Scancode.SDL_SCANCODE_5, new byte[]{0x06} },
    { SDL.SDL_Scancode.SDL_SCANCODE_6, new byte[]{0x07} },
    { SDL.SDL_Scancode.SDL_SCANCODE_7, new byte[]{0x08} },
    { SDL.SDL_Scancode.SDL_SCANCODE_8, new byte[]{0x09} },
    { SDL.SDL_Scancode.SDL_SCANCODE_9, new byte[]{0x0A} },
    { SDL.SDL_Scancode.SDL_SCANCODE_0, new byte[]{0x0B} },

    // Symbols
    { SDL.SDL_Scancode.SDL_SCANCODE_MINUS, new byte[]{0x0C} },
    { SDL.SDL_Scancode.SDL_SCANCODE_EQUALS, new byte[]{0x0D} },
    { SDL.SDL_Scancode.SDL_SCANCODE_BACKSPACE, new byte[]{0x0E} },
    { SDL.SDL_Scancode.SDL_SCANCODE_TAB, new byte[]{0x0F} },
    { SDL.SDL_Scancode.SDL_SCANCODE_LEFTBRACKET, new byte[]{0x1A} },
    { SDL.SDL_Scancode.SDL_SCANCODE_RIGHTBRACKET, new byte[]{0x1B} },
    { SDL.SDL_Scancode.SDL_SCANCODE_RETURN, new byte[]{0x1C} },
    { SDL.SDL_Scancode.SDL_SCANCODE_SEMICOLON, new byte[]{0x27} },
    { SDL.SDL_Scancode.SDL_SCANCODE_APOSTROPHE, new byte[]{0x28} },
    { SDL.SDL_Scancode.SDL_SCANCODE_GRAVE, new byte[]{0x29} },
    { SDL.SDL_Scancode.SDL_SCANCODE_BACKSLASH, new byte[]{0x2B} },
    { SDL.SDL_Scancode.SDL_SCANCODE_COMMA, new byte[]{0x33} },
    { SDL.SDL_Scancode.SDL_SCANCODE_PERIOD, new byte[]{0x34} },
    { SDL.SDL_Scancode.SDL_SCANCODE_SLASH, new byte[]{0x35} },
    { SDL.SDL_Scancode.SDL_SCANCODE_SPACE, new byte[]{0x39} },

    // Modifiers
    { SDL.SDL_Scancode.SDL_SCANCODE_LSHIFT, new byte[]{0x2A} },
    { SDL.SDL_Scancode.SDL_SCANCODE_RSHIFT, new byte[]{0x36} },
    { SDL.SDL_Scancode.SDL_SCANCODE_LCTRL, new byte[]{0x1D} },
    { SDL.SDL_Scancode.SDL_SCANCODE_RCTRL, new byte[]{0xE0,0x1D} },
    { SDL.SDL_Scancode.SDL_SCANCODE_LALT, new byte[]{0x38} },
    { SDL.SDL_Scancode.SDL_SCANCODE_RALT, new byte[]{0xE0,0x38} },

    // Locks
    { SDL.SDL_Scancode.SDL_SCANCODE_CAPSLOCK, new byte[]{0x3A} },
    { SDL.SDL_Scancode.SDL_SCANCODE_NUMLOCKCLEAR, new byte[]{0x45} },
    { SDL.SDL_Scancode.SDL_SCANCODE_SCROLLLOCK, new byte[]{0x46} },

    // Function keys
    { SDL.SDL_Scancode.SDL_SCANCODE_F1, new byte[]{0x3B} },
    { SDL.SDL_Scancode.SDL_SCANCODE_F2, new byte[]{0x3C} },
    { SDL.SDL_Scancode.SDL_SCANCODE_F3, new byte[]{0x3D} },
    { SDL.SDL_Scancode.SDL_SCANCODE_F4, new byte[]{0x3E} },
    { SDL.SDL_Scancode.SDL_SCANCODE_F5, new byte[]{0x3F} },
    { SDL.SDL_Scancode.SDL_SCANCODE_F6, new byte[]{0x40} },
    { SDL.SDL_Scancode.SDL_SCANCODE_F7, new byte[]{0x41} },
    { SDL.SDL_Scancode.SDL_SCANCODE_F8, new byte[]{0x42} },
    { SDL.SDL_Scancode.SDL_SCANCODE_F9, new byte[]{0x43} },
    { SDL.SDL_Scancode.SDL_SCANCODE_F10, new byte[]{0x44} },
    { SDL.SDL_Scancode.SDL_SCANCODE_F11, new byte[]{0x57} },
    { SDL.SDL_Scancode.SDL_SCANCODE_F12, new byte[]{0x58} },

    // Navigation (extended)
    { SDL.SDL_Scancode.SDL_SCANCODE_INSERT, new byte[]{0xE0,0x52} },
    { SDL.SDL_Scancode.SDL_SCANCODE_DELETE, new byte[]{0xE0,0x53} },
    { SDL.SDL_Scancode.SDL_SCANCODE_HOME, new byte[]{0xE0,0x47} },
    { SDL.SDL_Scancode.SDL_SCANCODE_END, new byte[]{0xE0,0x4F} },
    { SDL.SDL_Scancode.SDL_SCANCODE_PAGEUP, new byte[]{0xE0,0x49} },
    { SDL.SDL_Scancode.SDL_SCANCODE_PAGEDOWN, new byte[]{0xE0,0x51} },

    // Arrows (extended)
    { SDL.SDL_Scancode.SDL_SCANCODE_UP, new byte[]{0xE0,0x48} },
    { SDL.SDL_Scancode.SDL_SCANCODE_DOWN, new byte[]{0xE0,0x50} },
    { SDL.SDL_Scancode.SDL_SCANCODE_LEFT, new byte[]{0xE0,0x4B} },
    { SDL.SDL_Scancode.SDL_SCANCODE_RIGHT, new byte[]{0xE0,0x4D} },

    // Keypad
    { SDL.SDL_Scancode.SDL_SCANCODE_KP_0, new byte[]{0x52} },
    { SDL.SDL_Scancode.SDL_SCANCODE_KP_1, new byte[]{0x4F} },
    { SDL.SDL_Scancode.SDL_SCANCODE_KP_2, new byte[]{0x50} },
    { SDL.SDL_Scancode.SDL_SCANCODE_KP_3, new byte[]{0x51} },
    { SDL.SDL_Scancode.SDL_SCANCODE_KP_4, new byte[]{0x4B} },
    { SDL.SDL_Scancode.SDL_SCANCODE_KP_5, new byte[]{0x4C} },
    { SDL.SDL_Scancode.SDL_SCANCODE_KP_6, new byte[]{0x4D} },
    { SDL.SDL_Scancode.SDL_SCANCODE_KP_7, new byte[]{0x47} },
    { SDL.SDL_Scancode.SDL_SCANCODE_KP_8, new byte[]{0x48} },
    { SDL.SDL_Scancode.SDL_SCANCODE_KP_9, new byte[]{0x49} },

    { SDL.SDL_Scancode.SDL_SCANCODE_KP_PERIOD, new byte[]{0x53} },
    { SDL.SDL_Scancode.SDL_SCANCODE_KP_PLUS, new byte[]{0x4E} },
    { SDL.SDL_Scancode.SDL_SCANCODE_KP_MINUS, new byte[]{0x4A} },
    { SDL.SDL_Scancode.SDL_SCANCODE_KP_MULTIPLY, new byte[]{0x37} },
    { SDL.SDL_Scancode.SDL_SCANCODE_KP_DIVIDE, new byte[]{0xE0,0x35} },
    { SDL.SDL_Scancode.SDL_SCANCODE_KP_ENTER, new byte[]{0xE0,0x1C} },

    // 102nd ISO key
    { SDL.SDL_Scancode.SDL_SCANCODE_NONUSBACKSLASH, new byte[]{0x56} },

    // Windows keys
    { SDL.SDL_Scancode.SDL_SCANCODE_LGUI, new byte[]{0xE0,0x5B} },
    { SDL.SDL_Scancode.SDL_SCANCODE_RGUI, new byte[]{0xE0,0x5C} },
    { SDL.SDL_Scancode.SDL_SCANCODE_APPLICATION, new byte[]{0xE0,0x5D} },

    // Misc
    { SDL.SDL_Scancode.SDL_SCANCODE_ESCAPE, new byte[]{0x01} },
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

        foreach (var (scancode, scArr) in ScanCodes)
        {
            byte sc = scArr[0];
            bool extended = false;
            if (scArr[0] == 0xE0)
            {
                sc = scArr[1];
                extended = true;
            }

            int index = (int)scancode;
            bool down = Marshal.ReadByte(state, index) != 0;

            if (down)
            {
                if (!keys[index].down)
                {
                    
                    KeyDown(scancode, sc, extended);
                    keys[index].timer = repeatDelay;
                }
                else
                {
                    keys[index].timer -= updateDeltaTime / 1000f;

                    if (keys[index].timer <= 0)
                    {
                        KeyDown(scancode, sc, extended);
                        keys[index].timer = repeatRate;
                    }
                }
            }
            else if (keys[index].down)
            {
                KeyUp(scancode, sc, extended);
            }

            keys[index].down = down;
        }
    }

    void KeyDown(SDL.SDL_Scancode scancode, byte sc, bool extended)
    {
        Console.WriteLine($"Key {scancode} down");
        if (scancode == SDL.SDL_Scancode.SDL_SCANCODE_CAPSLOCK)
        {
            PushScancode(0x3A, extended);
            PushScancode(0xBA, extended);
            return;
        }

        PushScancode(sc, extended);
    }

    void KeyUp(SDL.SDL_Scancode scancode, byte sc, bool extended)
    {
        if (scancode == SDL.SDL_Scancode.SDL_SCANCODE_CAPSLOCK)
            return;
        PushScancode((byte)(sc | 0x80), extended);
    }

    void PushScancode(byte code, bool extended)
    {
        fifo.Enqueue(code);

        Cpu.instance.memoryBus.dev[baseAddress + REG_DATA] = code;
        Cpu.instance.memoryBus.dev[baseAddress + REG_DATA_EXTENDED] = (byte)(extended ? 0xE8 : 0);

        SetStatusBit(STATUS_OUTPUT_FULL);
        SetStatusBit(STATUS_IRQ_PENDING);

        InterruptController.instance.HandleInterrupt((byte)Cpu.IRQ_KEYBOARD);
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
        {
            //Cpu.instance.memoryBus.dev[baseAddress + REG_DATA] = fifo.Peek();
        }
        else
        {
            //Cpu.instance.memoryBus.dev[baseAddress + REG_DATA] = 0;
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
