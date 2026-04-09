namespace ViewModels.ModbusClient.Manual.DataTypes;

public class WriteData
{
    public readonly byte[] Data;
    public readonly int NumberOfRegisters;

    public WriteData(byte[] data, int numberOfRegisters)
    {
        Data = data;
        NumberOfRegisters = numberOfRegisters;
    }
}
