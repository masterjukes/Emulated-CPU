using System.Collections;
using System.Drawing.Imaging;

namespace Fun_CPU.Vga;

using System;
using Fun_CPU;

public class VgaDevice : MMIODevice
{
    public const int ControlByteSize = 1;
    public const int GraphicModeSize = 1024 * 768 * 2;
    public const int TextModeSize = 80 * 25 * 2;
    public override int size => ControlByteSize + GraphicModeSize + TextModeSize ;
    
    public override float updateDeltaTime => 33f;

    public int textModeBase;
    public int graphicsModeBase;
    
    public override void UpdateDevice()
    {
        textModeBase = baseAddress + ControlByteSize + GraphicModeSize;
        graphicsModeBase = baseAddress + ControlByteSize;

        
        ref var controlByte = ref Cpu.instance.memoryBus.dev[baseAddress];
        if(controlByte == 0)
            return;
        
        var textMode = (controlByte & 0x01) == 1;
        var cursorVisible = (controlByte & 0x02) == 2;
        var cursorBlinking = (controlByte & 0x04) == 4;
        

        if (textMode)
        {
            UpdateFrame();
            
        }
        
        UploadFramebuffer();
    }
    
    void UpdateFrame()
    {
        ref byte[] mem = ref Cpu.instance.memoryBus.dev;

        int baseAddr = graphicsModeBase;

        for (int y = 0; y < 768; y++)
        {
            for (int x = 0; x < 1024; x++)
            {
                byte r = (byte)(x * 255 / 1024);
                byte g = (byte)(y * 255 / 768);
                byte b = 128;

                ushort pixel =
                    (ushort)(
                        ((r >> 3) << 11) |
                        ((g >> 2) << 5) |
                        (b >> 3)
                    );

                
            }
        }
        
    }
    
    unsafe void UploadFramebuffer()
    {
        BitmapData data = Screen.instance.bitmap.LockBits(
            new Rectangle(0, 0, 1024, 768),
            ImageLockMode.WriteOnly,
            PixelFormat.Format32bppArgb
        );

        byte* dst = (byte*)data.Scan0;
        byte[] mem = Cpu.instance.memoryBus.dev;

        int baseAddr = graphicsModeBase;

        for (int i = 0; i < 1024 * 768; i++)
        {
            int addr = baseAddr + i * 2;

            ushort pixel = (ushort)(
                mem[addr] |
                (mem[addr + 1] << 8)
            );

            byte r = (byte)(((pixel >> 11) & 0x1F) * 255 / 31);
            byte g = (byte)(((pixel >> 5) & 0x3F) * 255 / 63);
            byte b = (byte)((pixel & 0x1F) * 255 / 31);

            dst[i * 4 + 0] = b;
            dst[i * 4 + 1] = g;
            dst[i * 4 + 2] = r;
            dst[i * 4 + 3] = 255;
        }

        Screen.instance.bitmap.UnlockBits(data);
        Screen.instance.Invalidate();

    }
}

public sealed class Screen : Form
{
    public static Screen instance = new();
    public readonly Bitmap bitmap;
    public Screen()
    {
        Width = 1024;
        Height = 768;
        
        bitmap = new Bitmap(
            Width,
            Height,
            PixelFormat.Format32bppArgb
        );

        FormBorderStyle = FormBorderStyle.FixedSingle;
        DoubleBuffered = true;
    }


    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.InterpolationMode =
            System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;

        e.Graphics.DrawImage(
            bitmap,
            new Rectangle(0, 0, 1024, 768)
        );
    }

}