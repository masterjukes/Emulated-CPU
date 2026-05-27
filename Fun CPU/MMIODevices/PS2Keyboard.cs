namespace Fun_CPU.Vga;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

public class PS2Keyboard : MMIODevice
{
    [DllImport("user32.dll")]
    static extern short GetAsyncKeyState(int vKey);

    private class KeyState
    {
        public bool down;
        public float timer;
    }
    
    KeyState[] keys = new KeyState[256]
        .Select(_ => new KeyState())
        .ToArray();

    Queue<byte> fifo = new();
    
    bool shift;
    bool ctrl;
    bool alt;
    bool capsLock;
    
    public override int size => 0x10;
    public override float updateDeltaTime => 2;

    const int REG_DATA = 0;
    const int REG_STATUS = 1;
    const int REG_COMMAND = 2;

    const byte STATUS_OUTPUT_FULL = 1 << 0;
    const byte STATUS_IRQ_PENDING = 1 << 1;
    
    float repeatDelay = 0.5f;
    float repeatRate = 0.05f;
    

Dictionary<int, byte> scanCodes = new()
{
    // Letters
    { 0x41, 0x1E }, // A
    { 0x42, 0x30 }, // B
    { 0x43, 0x2E }, // C
    { 0x44, 0x20 }, // D
    { 0x45, 0x12 }, // E
    { 0x46, 0x21 }, // F
    { 0x47, 0x22 }, // G
    { 0x48, 0x23 }, // H
    { 0x49, 0x17 }, // I
    { 0x4A, 0x24 }, // J
    { 0x4B, 0x25 }, // K
    { 0x4C, 0x26 }, // L
    { 0x4D, 0x32 }, // M
    { 0x4E, 0x31 }, // N
    { 0x4F, 0x18 }, // O
    { 0x50, 0x19 }, // P
    { 0x51, 0x10 }, // Q
    { 0x52, 0x13 }, // R
    { 0x53, 0x1F }, // S
    { 0x54, 0x14 }, // T
    { 0x55, 0x16 }, // U
    { 0x56, 0x2F }, // V
    { 0x57, 0x11 }, // W
    { 0x58, 0x2D }, // X
    { 0x59, 0x15 }, // Y
    { 0x5A, 0x2C }, // Z

    // Numbers (top row)
    { 0x30, 0x0B }, // 0
    { 0x31, 0x02 }, // 1
    { 0x32, 0x03 }, // 2
    { 0x33, 0x04 }, // 3
    { 0x34, 0x05 }, // 4
    { 0x35, 0x06 }, // 5
    { 0x36, 0x07 }, // 6
    { 0x37, 0x08 }, // 7
    { 0x38, 0x09 }, // 8
    { 0x39, 0x0A }, // 9

    // Controls
    { 0x0D, 0x1C }, // Enter
    { 0x1B, 0x01 }, // Esc
    { 0x08, 0x0E }, // Backspace
    { 0x09, 0x0F }, // Tab
    { 0x20, 0x39 }, // Space

    // Modifiers
    { 0x10, 0x2A }, // Shift
    { 0x11, 0x1D }, // Ctrl
    { 0x12, 0x38 }, // Alt

    // Function keys
    { 0x70, 0x3B }, // F1
    { 0x71, 0x3C }, // F2
    { 0x72, 0x3D }, // F3
    { 0x73, 0x3E }, // F4
    { 0x74, 0x3F }, // F5
    { 0x75, 0x40 }, // F6
    { 0x76, 0x41 }, // F7
    { 0x77, 0x42 }, // F8
    { 0x78, 0x43 }, // F9
    { 0x79, 0x44 }, // F10
    { 0x7A, 0x57 }, // F11
    { 0x7B, 0x58 }, // F12

    // Arrow keys (EXTENDED 0xE0)
    { 0x25, 0x4B }, // Left
    { 0x26, 0x48 }, // Up
    { 0x27, 0x4D }, // Right
    { 0x28, 0x50 }, // Down

    // Navigation (EXTENDED)
    { 0x24, 0x47 }, // Home
    { 0x23, 0x4F }, // End
    { 0x21, 0x49 }, // Page Up
    { 0x22, 0x51 }, // Page Down
    { 0x2D, 0x52 }, // Insert
    { 0x2E, 0x53 }, // Delete

    // Numpad (basic)
    { 0x60, 0x52 }, // Numpad 0
    { 0x61, 0x4F }, // Numpad 1
    { 0x62, 0x50 }, // Numpad 2
    { 0x63, 0x51 }, // Numpad 3
    { 0x64, 0x4B }, // Numpad 4
    { 0x65, 0x4C }, // Numpad 5
    { 0x66, 0x4D }, // Numpad 6
    { 0x67, 0x47 }, // Numpad 7
    { 0x68, 0x48 }, // Numpad 8
    { 0x69, 0x49 }, // Numpad 9

    { 0x6A, 0x37 }, // *
    { 0x6B, 0x4E }, // +
    { 0x6D, 0x4A }, // -
    { 0x6E, 0x53 }, // .
    { 0x6F, 0x35 }, // /

};

    public override void UpdateDevice()
    {

        PollKeyboard();
        HandleGuestCommands();
        UpdateRegisters();
    }

    void PollKeyboard()
    {
        for(int i = 0; i < 256; i++)
        {
            bool down = (GetAsyncKeyState(i) & 0x8000) != 0;

            if(down)
            {
                if(!keys[i].down)
                {
                    KeyDown(i);
                    keys[i].timer = repeatDelay;
                }
                else
                {
                    keys[i].timer -= updateDeltaTime / 1000f;

                    if(keys[i].timer <= 0)
                    {
                        KeyDown(i); // repeat
                        keys[i].timer = repeatRate;
                    }
                }
            }
            else if(keys[i].down)
            {
                KeyUp(i);
            }

            keys[i].down = down;
        } 
    }

    void KeyDown(int vk)
    {
        if(vk == 0x10) shift = true;
        if(vk == 0x11) ctrl  = true;
        if(vk == 0x12) alt   = true;
        
        if(vk == 0x14)
        {
            capsLock = !capsLock;
            PushScancode(0x3A); 
            PushScancode(0xBA); 
            return;
        }
        
        if(scanCodes.TryGetValue(vk, out byte sc))
        {
            PushScancode(sc);
        }
    }

    void KeyUp(int vk)
    {
        if(vk == 0x10) shift = false;
        if(vk == 0x11) ctrl  = false;
        if(vk == 0x12) alt   = false;
        
        
        if(vk == 0x14)
            return; 
        
        if(scanCodes.TryGetValue(vk, out byte sc))
        {
            PushScancode((byte)(sc | 0x80));
        }
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

        if(cmd == 0)
            return;

        switch(cmd)
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
        if(fifo.Count > 0)
        {
            Cpu.instance.memoryBus.dev[baseAddress + REG_DATA] = fifo.Peek();
        }
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

        if(offset != REG_DATA)
            return;
        
        if(fifo.Count == 0)
            return;

        fifo.Dequeue();

        if(fifo.Count == 0)
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