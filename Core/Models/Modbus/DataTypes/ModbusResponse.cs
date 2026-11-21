namespace Core.Models.Modbus.DataTypes;

public struct ModbusResponse
{
    public byte SlaveID;

    // PDU - Protocol Data Unit
    public PduResponse PDU;
}
