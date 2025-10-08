using Core.Models.Modbus;

namespace Core.Tests.Modbus;

public class CheckSum_CRC16_Test
{
    private const UInt16 Polynom = 0xA001;

    [Fact]
    public void Test_WriteSingleCoil_Request()
    {
        byte[] Data = new byte[] { 0x01, 0x05, 0x05, 0x00, 0xFF, 0x00, 0x00, 0x00 };

        UInt16 CheckSum_Expected = 0x8CF6;

        UInt16 CheckSum_Actual = GetCheckSum_Actual(Data);

        Assert.Equal(CheckSum_Expected, CheckSum_Actual);
    }

    [Fact]
    public void Test_ReadHoldingRegisters_Request()
    {
        byte[] Data = new byte[] { 0x01, 0x03, 0x0B, 0x00, 0x00, 0x02, 0x00, 0x00 };

        UInt16 CheckSum_Expected = 0xC62F;

        UInt16 CheckSum_Actual = GetCheckSum_Actual(Data);

        Assert.Equal(CheckSum_Expected, CheckSum_Actual);
    }

    [Fact]
    public void Test_WriteMultipleRegisters_Request()
    {
        byte[] Data = new byte[] { 0x01, 0x10, 0x0A, 0x05, 0x00, 0x02, 0x04, 0x41, 0x70, 0x00, 0x00, 0x00, 0x00 };

        UInt16 CheckSum_Expected = 0x58D7;

        UInt16 CheckSum_Actual = GetCheckSum_Actual(Data);

        Assert.Equal(CheckSum_Expected, CheckSum_Actual);
    }

    [Fact]
    public void Test_EmptyArray()
    {
        byte[] Data = new byte[] { };

        UInt16 CheckSum_Expected = 0xFFFF;

        UInt16 CheckSum_Actual = GetCheckSum_Actual(Data);

        Assert.Equal(CheckSum_Expected, CheckSum_Actual);
    }

    [Fact]
    public void Test_SingleByte()
    {
        byte[] Data = new byte[] { 0x01 };

        UInt16 CheckSum_Expected = 0xFFFF;

        UInt16 CheckSum_Actual = GetCheckSum_Actual(Data);

        Assert.Equal(CheckSum_Expected, CheckSum_Actual);
    }

    private UInt16 GetCheckSum_Actual(byte[] Data)
    {
        byte[] CheckSumArray = CheckSum.Calculate_CRC16(Data, Polynom);

        Array.Reverse(CheckSumArray);

        return BitConverter.ToUInt16(CheckSumArray);
    }
}
