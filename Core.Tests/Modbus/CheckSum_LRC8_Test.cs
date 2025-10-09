using Core.Models.Modbus;
using Core.Models.Modbus.Message;

namespace Core.Tests.Modbus;

public class CheckSum_LRC8_Test
{
    [Fact]
    public void Test_ReadCoils_Request()
    {
        byte[] Data = new byte[] { 0x01, 0x03, 0x02, 0x00, 0x00, 0x02, 0x00 };

        byte CheckSum_Expected = 0xF8;

        byte CheckSum_Actual = GetCheckSum_Actual(Data);

        Assert.Equal(CheckSum_Expected, CheckSum_Actual);
    }

    [Fact]
    public void Test_ReadInputRegisters_Request()
    {
        byte[] Data = new byte[] { 0x01, 0x03, 0x04, 0x00, 0xB1, 0x1F, 0x40, 0x00 };

        byte CheckSum_Expected = 0xE8;

        byte CheckSum_Actual = GetCheckSum_Actual(Data);

        Assert.Equal(CheckSum_Expected, CheckSum_Actual);
    }

    [Fact]
    public void Test_WriteMultipleCoils_Request()
    {
        byte[] Data = new byte[] { 0x01, 0x10, 0x01, 0x12, 0x00, 0x02, 0x00 };

        byte CheckSum_Expected = 0xDA;

        byte CheckSum_Actual = GetCheckSum_Actual(Data);

        Assert.Equal(CheckSum_Expected, CheckSum_Actual);
    }

    [Fact]
    public void Test_EmptyArray()
    {
        byte[] Data = Array.Empty<byte>();

        byte CheckSum_Expected = 0x00;

        byte CheckSum_Actual = GetCheckSum_Actual(Data);

        Assert.Equal(CheckSum_Expected, CheckSum_Actual);
    }

    [Fact]
    public void Test_SingleByte()
    {
        byte[] Data = new byte[] { 0x01 };

        byte CheckSum_Expected = 0xFF;

        byte CheckSum_Actual = GetCheckSum_Actual(Data);

        Assert.Equal(CheckSum_Expected, CheckSum_Actual);
    }

    private byte GetCheckSum_Actual(byte[] Data)
    {
        byte[] CheckSum_Actual_Array = CheckSum.Calculate_LRC8(Data);

        return ModbusASCII_Message.ConvertArrayToBytes(CheckSum_Actual_Array).First();
    }
}
