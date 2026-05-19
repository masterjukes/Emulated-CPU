namespace Fun_CPU;

public interface IBus
{
    byte ReadByte(int address);
    void WriteByte(int address, byte value);
}

public class SimulatedBusController : IBus
{
    private byte[] _memory;

    public byte ReadByte(int address)
    {
        return _memory[address];
    }

    public void WriteByte(int address, byte value)
    {
        _memory[address] = value;
    }
}