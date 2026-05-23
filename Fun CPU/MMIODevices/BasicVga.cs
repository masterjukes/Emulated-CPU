using System.Collections;
using System.Drawing.Imaging;
using System.Drawing.Imaging.Effects;
using System.Drawing.Text;

namespace Fun_CPU.Vga;

using System;
using Fun_CPU;

public class VgaDevice : MMIODevice
{
    public const int ControlByteSize = 1;
    public const int GraphicModeSize = 1024 * 768 * 2;
    public const int TextModeSize = 80 * 25 * 2;
    private byte currentTick;
    
    bool cursorBlinking = false;
    
    bool shouldBlink = false;
    public static bool textMode = false;
    
    Bitmap[] glyphs = new Bitmap[256];
    Bitmap glyphBuffer = new Bitmap(8, 16);
    
    
    private readonly PrivateFontCollection fonts = new();
    private Font textFont;
    public override int size => ControlByteSize + GraphicModeSize + TextModeSize ;

    public VgaDevice()
    {
        fonts.AddFontFile("PxPlus_IBM_VGA_8x16.ttf");
        
        textFont = new Font(
            fonts.Families[0],
            16,
            FontStyle.Regular,
            GraphicsUnit.Pixel
        );
        
        for (int c = 0; c < 256; c++)
        {
            Bitmap bmp = new Bitmap(8, 16);
            using Graphics g2 = Graphics.FromImage(bmp);

            g2.Clear(Color.Transparent);
            g2.TextRenderingHint = TextRenderingHint.SingleBitPerPixel;

            g2.DrawString(
                ((char)c).ToString(),
                textFont,
                Brushes.White,
                -2,
                -2
            );

            glyphs[c] = bmp;
        }
    }
    public override float updateDeltaTime => 33f;

    public int textModeBase;
    public int graphicsModeBase;
    
    private readonly Brush[] palette =
    {
        Brushes.Black,
        Brushes.Blue,
        Brushes.Green,
        Brushes.Cyan,
        Brushes.Red,
        Brushes.Magenta,
        Brushes.Brown,
        Brushes.LightGray,
        Brushes.DarkGray,
        Brushes.LightBlue,
        Brushes.LightGreen,
        Brushes.LightCyan,
        Brushes.LightCoral,
        Brushes.Violet,
        Brushes.Yellow,
        Brushes.White
    };
    
    public override void UpdateDevice()
    {
        textModeBase = baseAddress + ControlByteSize + GraphicModeSize;
        graphicsModeBase = baseAddress + ControlByteSize;
        
        if(currentTick % 30 == 0)
            shouldBlink = !shouldBlink;
        
        ref var controlByte = ref Cpu.instance.memoryBus.dev[baseAddress];
        if(controlByte == 0)
            return;
        
        textMode = (controlByte & 0x01) == 1;
        var cursorVisible = (controlByte & 0x02) == 2;
        cursorBlinking = (controlByte & 0x04) == 4;
        
        if (textMode)
            RenderTextMode();
        else
            UploadFramebuffer();
        
        currentTick++;
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
    
    
    void RenderTextMode()
    {
        byte[] mem = Cpu.instance.memoryBus.dev;

        const int columns = 80;
        const int rows = 25;

        int charWidth = 8;
        int charHeight = 16;

        using Graphics g = Graphics.FromImage(Screen.instance.textBuffer);

        g.Clear(Color.Black);

        g.PixelOffsetMode =
            System.Drawing.Drawing2D.PixelOffsetMode.Half;

        g.SmoothingMode =
            System.Drawing.Drawing2D.SmoothingMode.None;

        g.TextRenderingHint =
            System.Drawing.Text.TextRenderingHint.SingleBitPerPixel;

        for (int y = 0; y < rows; y++)
        {
            for (int x = 0; x < columns; x++)
            {
                int index = textModeBase + ((y * columns + x) * 2);

                byte character = mem[index];
                byte attribute = mem[index + 1];
                
                int fg = attribute & 0x0F;
                int bg = (attribute >> 4) & 0b0000_0111;
                if(!cursorBlinking)
                    bg = (attribute >> 4) & 0b0000_1111;
                
                bool blink = (attribute & 0b1000_0000) > 0;
                
                

                Brush fgBrush = palette[fg];
                Brush bgBrush = palette[bg];
                
                int screenOffsetX = 0;
                int screenOffsetY = 1;
                
                int px = screenOffsetX + x * charWidth;
                int py = screenOffsetY + y * charHeight;

                

                
                // background
                g.FillRectangle(
                    bgBrush,
                    px,
                    py,
                    8,
                    12
                );

                if (cursorBlinking && blink && shouldBlink)
                {
                    g.DrawImage(
                        glyphs[32],
                        new Rectangle(px, py, 8, 16),
                        0, 0, 8, 16,
                        GraphicsUnit.Pixel );
                    break;
                }
                
                g.DrawImage(
                    glyphs[character],
                    new Rectangle(px, py, 8, 16),
                    0, 0, 8, 16,
                    GraphicsUnit.Pixel );
                

            }
        }

        Screen.instance.Invalidate();
    }
    
    
}

public sealed class Screen : Form
{
    public static Screen instance = new();
    public readonly Bitmap bitmap;
    public readonly Bitmap textBuffer =
        new Bitmap(640, 480);
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
        if (!VgaDevice.textMode)
        {
            e.Graphics.InterpolationMode =
                System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;

            e.Graphics.DrawImage(
                bitmap,
                new Rectangle(0, 0, 1024, 768)
            );
        }
        else
        {

            e.Graphics.InterpolationMode =
                System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;

            e.Graphics.PixelOffsetMode =
                System.Drawing.Drawing2D.PixelOffsetMode.Half;

            e.Graphics.DrawImage(
                textBuffer,
                new Rectangle(0, 0, 1024, 768),
                new Rectangle(0, 0, 640, 480),
                GraphicsUnit.Pixel
            );
        }
    }

}