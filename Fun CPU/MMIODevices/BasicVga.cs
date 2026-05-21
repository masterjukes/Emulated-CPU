using System.Collections;

namespace Fun_CPU.Vga;

using System;
using Fun_CPU;

public class VgaDevice : MMIODevice
{
    public const int ControlByteSize = 1;
    public const int GraphicModeSize = 1024 * 768 * 3;
    public const int TextModeSize = 80 * 25 * 2;
    public override int size => ControlByteSize + GraphicModeSize + TextModeSize ;
    
    public override float updateDeltaTime => 33f;
    
    
    public override void UpdateDevice()
    {
        var controlByte = Cpu.instance.memoryBus.dev[baseAddress];
        if(controlByte == 0)
            return;
        
        var textMode = (controlByte & 0x01) == 1;
        var cursorVisible = (controlByte & 0x02) == 2;
        var cursorBlinking = (controlByte & 0x04) == 4;
        
        var textModeBase = baseAddress + ControlByteSize + GraphicModeSize;
        var graphicsModeBase = baseAddress + ControlByteSize;

        if (textMode)
        {

            for (int i = 0; i < 80 * 25; i += 2)
            {
                Cpu.instance.memoryBus.dev[textModeBase];
                Cpu.instance.memoryBus.dev[textModeBase + 1];
            }
        }
        
    }
}