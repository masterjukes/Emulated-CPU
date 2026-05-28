using Fun_CPU;

namespace Fun_CPU.Vga;

public class VgaDevice : MMIODevice
{
    public const int ControlByteSize = 1;
    public const int GraphicModeSize = 1024 * 768 * 2;
    public const int TextModeSize = 80 * 40 * 2;

    private byte currentTick;
    private bool shouldBlink;

    bool cursorBlinking;

    readonly byte[][] glyphs;

    public override int size => ControlByteSize + GraphicModeSize + TextModeSize;
    public override float updateDeltaTime => 33f;

    public int textModeBase;
    public int graphicsModeBase;

    static readonly (byte b, byte g, byte r)[] Palette =
    {
        (0, 0, 0),
        (255, 0, 0),
        (0, 255, 0),
        (255, 255, 0),
        (0, 0, 255),
        (255, 0, 255),
        (128, 64, 0),
        (192, 192, 192),
        (64, 64, 64),
        (255, 128, 128),
        (128, 255, 128),
        (128, 255, 255),
        (240, 128, 128),
        (238, 130, 238),
        (255, 255, 0),
        (255, 255, 255)
    };

    public VgaDevice()
    {
        glyphs = VgaRomFont.BuildGlyphs();
    }

    public override void UpdateDevice()
    {
        textModeBase = baseAddress + ControlByteSize + GraphicModeSize;
        graphicsModeBase = baseAddress + ControlByteSize;

        if (currentTick % 30 == 0)
            shouldBlink = !shouldBlink;

        ref var controlByte = ref Cpu.instance.memoryBus.dev[baseAddress];
        if (controlByte == 0)
            return;

        Screen.TextMode = (controlByte & 0x01) == 1;
        cursorBlinking = (controlByte & 0x04) == 4;

        if (Screen.TextMode)
            RenderTextMode();
        else
            UploadFramebuffer();

        Screen.Dirty = true;
        currentTick++;
    }

    unsafe void UploadFramebuffer()
    {
        byte[] mem = Cpu.instance.memoryBus.dev;
        int baseAddr = graphicsModeBase;
        byte[] dst = Screen.Framebuffer;

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

            int o = i * 4;
            dst[o + 0] = b;
            dst[o + 1] = g;
            dst[o + 2] = r;
            dst[o + 3] = 255;
        }
    }

    void RenderTextMode()
    {
        byte[] mem = Cpu.instance.memoryBus.dev;
        byte[] buf = Screen.TextBuffer;

        const int columns = 80;
        const int rows = 40;
        const int charWidth = 8;
        const int charHeight = 16;

        Array.Clear(buf);

        for (int y = 0; y < rows; y++)
        {
            for (int x = 0; x < columns; x++)
            {
                int index = textModeBase + ((y * columns + x) * 2);

                byte character = mem[index];
                byte attribute = mem[index + 1];

                int fg = attribute & 0x0F;
                int bg = (attribute >> 4) & 0b0000_0111;
                if (!cursorBlinking)
                    bg = (attribute >> 4) & 0b0000_1111;

                bool blink = (attribute & 0b1000_0000) > 0;

                var fgColor = Palette[fg];
                var bgColor = Palette[bg];

                int screenOffsetX = 0;
                int screenOffsetY = 1;

                int px = screenOffsetX + x * charWidth;
                int py = screenOffsetY + y * charHeight;

                if (x > 0)
                    px += 1 * x;
                if (y > 0)
                    py -= 4 * y;

                FillRect(buf, Screen.TextWidth, px, py - 1, 9, 12, bgColor);

                if (cursorBlinking && blink && shouldBlink)
                {
                    BlitGlyph(buf, Screen.TextWidth, glyphs[32], px, py, fgColor);
                    break;
                }

                BlitGlyph(buf, Screen.TextWidth, glyphs[character], px, py, fgColor);
            }
        }
    }

    static void FillRect(byte[] buf, int stride, int x, int y, int w, int h, (byte b, byte g, byte r) color)
    {
        for (int row = y; row < y + h; row++)
        {
            if (row < 0 || row >= Screen.TextHeight)
                continue;

            for (int col = x; col < x + w; col++)
            {
                if (col < 0 || col >= stride)
                    continue;

                int o = (row * stride + col) * 4;
                buf[o] = color.b;
                buf[o + 1] = color.g;
                buf[o + 2] = color.r;
                buf[o + 3] = 255;
            }
        }
    }

    static void BlitGlyph(byte[] buf, int stride, byte[] glyph, int px, int py, (byte b, byte g, byte r) fg)
    {
        for (int j = 0; j < 16; j++)
        {
            int row = py + j;
            if (row < 0 || row >= Screen.TextHeight)
                continue;

            for (int i = 0; i < 8; i++)
            {
                if (glyph[j * 8 + i] == 0)
                    continue;

                int col = px + i;
                if (col < 0 || col >= stride)
                    continue;

                int o = (row * stride + col) * 4;
                buf[o] = fg.b;
                buf[o + 1] = fg.g;
                buf[o + 2] = fg.r;
                buf[o + 3] = 255;
            }
        }
    }
}
