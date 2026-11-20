namespace Core.Models.Modbus.DataTypes;

public struct RawResponse
{
    public byte[] ResponseBytes;
    public readonly byte ExpectedSlaveID;
    public readonly bool CheckSumIsEnable;

    public RawResponse(byte[] responseBytes, byte expectedSlaveId, bool checkSumIsEnable)
    {
        ResponseBytes = responseBytes;
        ExpectedSlaveID = expectedSlaveId;
        CheckSumIsEnable = checkSumIsEnable;
    }
}
