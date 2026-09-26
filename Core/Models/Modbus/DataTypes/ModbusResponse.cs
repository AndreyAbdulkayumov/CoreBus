namespace Core.Models.Modbus.DataTypes;

public struct ModbusResponse
{
    // Только для Modbus TCP
    public ushort OperationNumber;
    public ushort ProtocolID;
    public ushort Length;

    // Общая часть для всех типов Modbus протокола
    public byte SlaveID;

    // PDU - Protocol Data Unit
    public PduResponse PDU;
}
