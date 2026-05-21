namespace Fun_CPU.Vga;

using System;
using Fun_CPU;
using System.Drawing;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

public partial class Form1 : Form
{
    const int ScreenWidth = 320;
    const int ScreenHeight = 200;

    private byte[] framebuffer => VgaDevice.instance.Framebuffer;

    private readonly Bitmap bitmap;

    public Form1()
    {

        Width = 1280;
        Height = 800;

        DoubleBuffered = true;

        bitmap = new Bitmap(
            ScreenWidth,
            ScreenHeight,
            PixelFormat.Format32bppArgb
        );

        var timer = new System.Windows.Forms.Timer();
        timer.Interval = 16;

        timer.Tick += (_, _) =>
        {
            UpdateFrame();
            UploadFramebuffer();
            Invalidate();
        };

        timer.Start();
    }

    void UpdateFrame()
    {
        for (int y = 0; y < ScreenHeight; y++)
        {
            for (int x = 0; x < ScreenWidth; x++)
            {
                int i = (y * ScreenWidth + x) * 4;

                byte r = (byte)x;
                byte g = (byte)y;
                byte b = 0;

                framebuffer[i + 0] = b;
                framebuffer[i + 1] = g;
                framebuffer[i + 2] = r;
                framebuffer[i + 3] = 255;
            }
        }
    }

    unsafe void UploadFramebuffer()
    {
        BitmapData data = bitmap.LockBits(
            new Rectangle(0, 0, bitmap.Width, bitmap.Height),
            ImageLockMode.WriteOnly,
            PixelFormat.Format32bppArgb
        );

        fixed (byte* src = framebuffer)
        {
            Buffer.MemoryCopy(
                src,
                (void*)data.Scan0,
                framebuffer.Length,
                framebuffer.Length
            );
        }

        bitmap.UnlockBits(data);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.InterpolationMode =
            System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;

        e.Graphics.DrawImage(
            bitmap,
            new Rectangle(0, 0, Width, Height)
        );
    }
}

public class VgaDevice : IMemoryRegion
{
    public static VgaDevice instance = new VgaDevice();
    public const int Width = 320;
    public const int Height = 200;

    private readonly byte[] framebuffer =
        new byte[Width * Height * 4];

    public byte[] Framebuffer => framebuffer;

    void Render()
    {
        

    }
    
    public byte ReadByte(uint offset)
    {
        if (offset >= framebuffer.Length)
            return 0;

        return framebuffer[offset];
    }

    public void WriteByte(uint offset, byte value)
    {
        if (offset >= framebuffer.Length)
            return;

        framebuffer[offset] = value;
    }

    public int ReadWord(uint offset)
    {
        if (offset + 4 > framebuffer.Length)
            return 0;

        return BitConverter.ToInt32(framebuffer, (int)offset);
    }

    public void WriteWord(uint offset, int value)
    {
        if (offset + 4 > framebuffer.Length)
            return;

        framebuffer[offset + 0] = (byte)(value);
        framebuffer[offset + 1] = (byte)(value >> 8);
        framebuffer[offset + 2] = (byte)(value >> 16);
        framebuffer[offset + 3] = (byte)(value >> 24);
    }
}