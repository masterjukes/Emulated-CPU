using Fun_CPU.Vga;

namespace Fun_CPU;

class Program
{
    static void Main()
    {
        ApplicationConfiguration.Initialize();
        Application.Run(new Form1());
        while (true)
        {
            for (int i = 0; i < 5_000_000; i++)
            {
                Cpu.instance.StepClock();
            }
            Console.Clear();
            Console.WriteLine(Cpu.instance.controlStatusRegisters.cycle.value);
        }
    }
}




