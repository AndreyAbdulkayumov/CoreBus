namespace Core.Models.Modbus.DataTypes;

public abstract class ModbusRequestData
{
    public byte SlaveID;
    public ushort Address;
    public bool CheckSum_IsEnable;
    public int NumberOfRegisters;
}

public class ModbusReadRequestData : ModbusRequestData
{
    public ModbusReadRequestData(byte slaveID, ushort address, int numberOfRegisters, bool checkSum_IsEnable)
    {
        SlaveID = slaveID;
        Address = address;
        NumberOfRegisters = numberOfRegisters;
        CheckSum_IsEnable = checkSum_IsEnable;
    }
}

public class ModbusWriteRequestData : ModbusRequestData
{
    public byte[] WriteData;

    public ModbusWriteRequestData(byte slaveID, ushort address, byte[] writeData, int numberOfRegisters, bool checkSum_IsEnable)
    {
        SlaveID = slaveID;
        Address = address;
        WriteData = writeData;
        NumberOfRegisters = numberOfRegisters;
        CheckSum_IsEnable = checkSum_IsEnable;
    }
}
