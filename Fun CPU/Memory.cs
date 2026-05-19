namespace Fun_CPU;

public interface IBus
{
    byte ReadByte(int address);
    void WriteByte(int address, byte value);
}

public class SimulatedBusController : IBus
{
    private byte[] dataFlow;

    public byte ReadByte(int address)
    {
        return dataFlow[address];
    }

    public void WriteByte(int address, byte value)
    {
        dataFlow[address] = value;
    }
}