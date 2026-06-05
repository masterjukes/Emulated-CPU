using SDL2;

namespace Fun_CPU.Vga;

public static class Screen
{
    public const int Width = 1024;
    public const int Height = 768;
    public const int TextWidth = 640;
    public const int TextHeight = 480;

    public static readonly byte[] Framebuffer = new byte[Width * Height * 4];
    public static readonly byte[] TextBuffer = new byte[TextWidth * TextHeight * 4];

    public static volatile bool TextMode;
    public static volatile bool Dirty = true;
    public static bool QuitRequested { get; private set; }

    static IntPtr _window;
    static IntPtr _renderer;
    static IntPtr _frameTexture;
    static IntPtr _textTexture;

    public static void Init()
    {
        if (SDL.SDL_Init(SDL.SDL_INIT_VIDEO) < 0)
            throw new InvalidOperationException($"SDL_Init failed: {SDL.SDL_GetError()}");

        _window = SDL.SDL_CreateWindow(
            "Fun CPU",
            SDL.SDL_WINDOWPOS_CENTERED,
            SDL.SDL_WINDOWPOS_CENTERED,
            Width,
            Height,
            SDL.SDL_WindowFlags.SDL_WINDOW_SHOWN);

        if (_window == IntPtr.Zero)
            throw new InvalidOperationException($"SDL_CreateWindow failed: {SDL.SDL_GetError()}");

        _renderer = SDL.SDL_CreateRenderer(
            _window,
            -1,
            SDL.SDL_RendererFlags.SDL_RENDERER_ACCELERATED | SDL.SDL_RendererFlags.SDL_RENDERER_PRESENTVSYNC);

        if (_renderer == IntPtr.Zero)
            throw new InvalidOperationException($"SDL_CreateRenderer failed: {SDL.SDL_GetError()}");

        _frameTexture = SDL.SDL_CreateTexture(
            _renderer,
            SDL.SDL_PIXELFORMAT_ARGB8888,
            (int)SDL.SDL_TextureAccess.SDL_TEXTUREACCESS_STREAMING,
            Width,
            Height);

        _textTexture = SDL.SDL_CreateTexture(
            _renderer,
            SDL.SDL_PIXELFORMAT_ARGB8888,
            (int)SDL.SDL_TextureAccess.SDL_TEXTUREACCESS_STREAMING,
            TextWidth,
            TextHeight);

        if (_frameTexture == IntPtr.Zero || _textTexture == IntPtr.Zero)
            throw new InvalidOperationException($"SDL_CreateTexture failed: {SDL.SDL_GetError()}");
    }

    public static void ProcessEvents()
    {
        SDL.SDL_PumpEvents();

        while (SDL.SDL_PollEvent(out SDL.SDL_Event evt) != 0)
        {
            if (evt.type == SDL.SDL_EventType.SDL_QUIT)
                QuitRequested = true;
        }
    }

    public static void Present()
    {
        if (!Dirty)
            return;

        Dirty = false;

        if (TextMode)
            UploadTexture(_textTexture, TextBuffer, TextWidth, TextHeight);
        else
            UploadTexture(_frameTexture, Framebuffer, Width, Height);

        SDL.SDL_RenderClear(_renderer);

        if (TextMode)
        {
            var src = new SDL.SDL_Rect { x = 0, y = 0, w = TextWidth, h = TextHeight };
            var dst = new SDL.SDL_Rect { x = 0, y = 0, w = Width, h = Height };
            SDL.SDL_RenderCopy(_renderer, _textTexture, ref src, ref dst);
        }
        else
        {
            SDL.SDL_RenderCopy(_renderer, _frameTexture, IntPtr.Zero, IntPtr.Zero);
        }

        SDL.SDL_RenderPresent(_renderer);
    }

    static void UploadTexture(IntPtr texture, byte[] pixels, int w, int h)
    {
        SDL.SDL_LockTexture(texture, IntPtr.Zero, out IntPtr texPixels, out int pitch);
        int rowBytes = w * 4;
        for (int y = 0; y < h; y++)
            System.Runtime.InteropServices.Marshal.Copy(
                pixels,
                y * rowBytes,
                texPixels + y * pitch,
                rowBytes);
        SDL.SDL_UnlockTexture(texture);
    }

    public static void Shutdown()
    {
        if (_frameTexture != IntPtr.Zero) SDL.SDL_DestroyTexture(_frameTexture);
        if (_textTexture != IntPtr.Zero) SDL.SDL_DestroyTexture(_textTexture);
        if (_renderer != IntPtr.Zero) SDL.SDL_DestroyRenderer(_renderer);
        if (_window != IntPtr.Zero) SDL.SDL_DestroyWindow(_window);
        SDL.SDL_Quit();
    }
}
