using System.Diagnostics;
using Fun_CPU.Vga;
using Screen = Fun_CPU.Vga.Screen;
using Timer = System.Threading.Timer;

namespace Fun_CPU;

class Program
{
    [STAThread]
    
    static void Main()
    {
        DeviceThread();
        CpuThread();

        ApplicationConfiguration.Initialize();
        Application.Run(Screen.instance);
        
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
                {
                    Cpu.instance.StepClock();
                    //Thread.Sleep(100);
                }
                Console.Clear();
                Console.WriteLine(Cpu.instance.controlStatusRegisters.cycle.value);
            }
        }).Start();
    }


    static void DeviceThread()
    {
        var vga = new VgaDevice();

        new Thread(() =>
        {
            while (true)
            {
                foreach (var dev in MMIODevice.devices)
                {
                    dev.UpdateDevice();
                }

                Thread.Sleep(16);
            }
        }).Start();
    }
}




