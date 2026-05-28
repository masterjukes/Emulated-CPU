using Fun_CPU.Vga;

namespace Fun_CPU;

class Program
{
    static void Main()
    {
        Screen.Init();
        DeviceThread();
        CpuThread();

        while (!Screen.QuitRequested)
        {
            Screen.ProcessEvents();
            Screen.Present();
            Thread.Sleep(1);
        }

        Screen.Shutdown();
    }

    static void CpuThread()
    {
        Cpu.instance.PC = 0x7FFF0000;
        Cpu.instance.nextPC = 0x7FFF0000;
        new Thread(() =>
        {
            while (true)
            {
                for (int i = 0; i < 1_000_000; i++)
                    Cpu.instance.StepClock();
            }
        }).Start();
    }

    static void DeviceThread()
    {
        _ = new VgaDevice();
        _ = new PS2Keyboard();

        foreach (var dev in MMIODevice.devices)
        {
            new Thread(() =>
            {
                while (true)
                {
                    dev.UpdateDevice();
                    Thread.Sleep((int)dev.updateDeltaTime);
                }
            }).Start();
        }
    }
}
