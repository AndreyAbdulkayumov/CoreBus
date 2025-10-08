using Core.Models.Modbus.DataTypes;
using Core.Models.Modbus.Message;

namespace Core.Tests.Modbus;

public class Protocol_TCP_CreateTest
{
    private ModbusMessage Message = new ModbusTCP_Message();

    // PackageNumber делать всегда равным 0

    [Fact]
    public void Test_ReadCoilStatus_CreatesCorrectMessage()
    {
        // Expected Modbus TCP message (MBAP Header + PDU):
        // Transaction ID: 00 00
        // Protocol ID:    00 00
        // Length:         00 06 (Unit ID + PDU length)
        // Unit ID:        9C (156)
        // PDU:            01 00 0C 00 05 (Function Code 01, Address 000C, Quantity 0005)
        // Full Message:   00 00 00 00 00 06 9C 01 00 0C 00 05
        CheckReadFunction(
            SelectedFunction: Function.ReadCoilStatus,
            PackageNumber: 0,
            SlaveID: 156,
            Address: 12,
            NumberOfRegisters: 5
            );
    }

    [Fact]
    public void Test_ReadDiscreteInputs_CreatesCorrectMessage()
    {
        // Expected Modbus TCP message (MBAP Header + PDU):
        // Transaction ID: 00 00
        // Protocol ID:    00 00
        // Length:         00 06 (Unit ID + PDU length)
        // Unit ID:        AC (172)
        // PDU:            02 00 0C 00 02 (Function Code 02, Address 000C, Quantity 0002)
        // Full Message:   00 00 00 00 00 06 AC 02 00 0C 00 02
        CheckReadFunction(
            SelectedFunction: Function.ReadDiscreteInputs,
            PackageNumber: 0,
            SlaveID: 172,
            Address: 12,
            NumberOfRegisters: 2
            );
    }

    [Fact]
    public void Test_ReadHoldingRegisters_CreatesCorrectMessage()
    {
        // Expected Modbus TCP message (MBAP Header + PDU):
        // Transaction ID: 00 00
        // Protocol ID:    00 00
        // Length:         00 06 (Unit ID + PDU length)
        // Unit ID:        10 (16)
        // PDU:            03 00 2A 00 05 (Function Code 03, Address 002A, Quantity 0005)
        // Full Message:   00 00 00 00 00 06 10 03 00 2A 00 05
        CheckReadFunction(
            SelectedFunction: Function.ReadHoldingRegisters,
            PackageNumber: 0,
            SlaveID: 16,
            Address: 42,
            NumberOfRegisters: 5
            );
    }

    [Fact]
    public void Test_ReadInputRegisters_CreatesCorrectMessage()
    {
        // Expected Modbus TCP message (MBAP Header + PDU):
        // Transaction ID: 00 00
        // Protocol ID:    00 00
        // Length:         00 06 (Unit ID + PDU length)
        // Unit ID:        06 (6)
        // PDU:            04 00 13 00 04 (Function Code 04, Address 0013, Quantity 0004)
        // Full Message:   00 00 00 00 00 06 06 04 00 13 00 04
        CheckReadFunction(
            SelectedFunction: Function.ReadInputRegisters,
            PackageNumber: 0,
            SlaveID: 6,
            Address: 19,
            NumberOfRegisters: 4
            );
    }

    [Fact]
    public void Test_ForceSingleCoil_CreatesCorrectMessage()
    {
        // Expected Modbus TCP message (MBAP Header + PDU):
        // Transaction ID: 00 00
        // Protocol ID:    00 00
        // Length:         00 06 (Unit ID + PDU length)
        // Unit ID:        0D (13)
        // PDU:            05 00 60 FF 00 (Function Code 05, Address 0060, WriteData FF00)
        // Full Message:   00 00 00 00 00 06 0D 05 00 60 FF 00
        CheckSingleWriteFunction(
            SelectedFunction: Function.ForceSingleCoil,
            PackageNumber: 0,
            SlaveID: 13,
            Address: 96,
            WriteData: 0xFF00
            );
    }

    [Fact]
    public void Test_PresetSingleRegister_CreatesCorrectMessage()
    {
        // Expected Modbus TCP message (MBAP Header + PDU):
        // Transaction ID: 00 00
        // Protocol ID:    00 00
        // Length:         00 06 (Unit ID + PDU length)
        // Unit ID:        12 (18)
        // PDU:            06 00 3F AE D5 (Function Code 06, Address 003F, WriteData AED5)
        // Full Message:   00 00 00 00 00 06 12 06 00 3F AE D5
        CheckSingleWriteFunction(
            SelectedFunction: Function.PresetSingleRegister,
            PackageNumber: 0,
            SlaveID: 18,
            Address: 63,
            WriteData: 0xAED5
            );
    }

    [Fact]
    public void Test_ForceMultipleCoils_CreatesCorrectMessage()
    {
        // Expected Modbus TCP message (MBAP Header + PDU):
        // Transaction ID: 00 00
        // Protocol ID:    00 00
        // Length:         00 0A (Unit ID + PDU length: 1+1+2+2+1+1 = 8 bytes for PDU, +1 for Unit ID = 9 bytes)
        // Unit ID:        20 (32)
        // PDU:            0F 00 49 00 07 01 0B (Function Code 0F, Address 0049, Quantity 0007, Byte Count 01, Coil Data 0B)
        // Full Message:   00 00 00 00 00 09 20 0F 00 49 00 07 01 0B
        CheckMultiplyWriteCoilsFunction(
            packageNumber: 0,
            slaveID: 32,
            address: 73,
            bitArray: new int[] { 0, 0, 1, 1, 1, 0, 1 }
            );
    }

    [Fact]
    public void Test_PresetMultipleRegisters_CreatesCorrectMessage()
    {
        // Expected Modbus TCP message (MBAP Header + PDU):
        // Transaction ID: 00 00
        // Protocol ID:    00 00
        // Length:         00 11 (Unit ID + PDU length: 1+1+2+2+1+5*2 = 17 bytes for PDU, +1 for Unit ID = 18 bytes)
        // Unit ID:        12 (18)
        // PDU:            10 00 3F 00 05 0A FF FF 45 86 40 00 05 68 FA FD
        // Full Message:   00 00 00 00 00 11 12 10 00 3F 00 05 0A FF FF 45 86 40 00 05 68 FA FD
        CheckMultiplyWriteRegistersFunction(
            PackageNumber: 0,
            SlaveID: 18,
            Address: 63,
            WriteData: new UInt16[] { 0xFFFF, 0x4586, 0x4000, 0x0568, 0xFAFD },
            );
    }

    [Fact]
    public void Test_ReadCoilStatus_WithSpecificPackageNumber_CreatesCorrectMessage()
    {
        // Expected Modbus TCP message (MBAP Header + PDU):
        // Transaction ID: 01 00
        // Protocol ID:    00 00
        // Length:         00 06 (Unit ID + PDU length)
        // Unit ID:        9C (156)
        // PDU:            01 00 0C 00 05 (Function Code 01, Address 000C, Quantity 0005)
        // Full Message:   01 00 00 00 00 06 9C 01 00 0C 00 05
        CheckReadFunction(
            SelectedFunction: Function.ReadCoilStatus,
            PackageNumber: 256,
            SlaveID: 156,
            Address: 12,
            NumberOfRegisters: 5
            );
    }

    [Fact]
    public void Test_ReadCoilStatus_MaxPackageNumber_CreatesCorrectMessage()
    {
        // Expected Modbus TCP message (MBAP Header + PDU):
        // Transaction ID: FF FF
        // Protocol ID:    00 00
        // Length:         00 06 (Unit ID + PDU length)
        // Unit ID:        9C (156)
        // PDU:            01 00 0C 00 05 (Function Code 01, Address 000C, Quantity 0005)
        // Full Message:   FF FF 00 00 00 06 9C 01 00 0C 00 05
        CheckReadFunction(
            SelectedFunction: Function.ReadCoilStatus,
            PackageNumber: 65535,
            SlaveID: 156,
            Address: 12,
            NumberOfRegisters: 5
            );
    }

    [Fact]
    public void Test_ReadCoilStatus_InvalidSlaveID_Zero_ThrowsException()
    {
        // SlaveID 0 is typically reserved for broadcast and should not be used as a unit address.
        // Expecting an exception or a specific error handling.
        Assert.Throws<Exception>(() => CheckReadFunction(
            SelectedFunction: Function.ReadCoilStatus,
            PackageNumber: 0,
            SlaveID: 0,
            Address: 12,
            NumberOfRegisters: 5
            ));
    }

    [Fact]
    public void Test_ReadCoilStatus_InvalidSlaveID_MaxByte_ThrowsException()
    {
        // SlaveID 255 is also an invalid unit address, often used for broadcast in some contexts.
        // Expecting an exception or a specific error handling.
        Assert.Throws<Exception>(() => CheckReadFunction(
            SelectedFunction: Function.ReadCoilStatus,
            PackageNumber: 0,
            SlaveID: 255,
            Address: 12,
            NumberOfRegisters: 5
            ));
    }

    [Fact]
    public void Test_ReadHoldingRegisters_MaxAddress_OneRegister_CreatesCorrectMessage()
    {
        // Expected Modbus TCP message for Address 0xFFFF and 1 register
        // Transaction ID: 00 00
        // Protocol ID:    00 00
        // Length:         00 06
        // Unit ID:        10 (16)
        // PDU:            03 FF FF 00 01
        // Full Message:   00 00 00 00 00 06 10 03 FF FF 00 01
        CheckReadFunction(
            SelectedFunction: Function.ReadHoldingRegisters,
            PackageNumber: 0,
            SlaveID: 16,
            Address: 0xFFFF,
            NumberOfRegisters: 1
            );
    }

    [Fact]
    public void Test_ReadHoldingRegisters_ZeroRegisters_ThrowsException()
    {
        // NumberOfRegisters = 0 should ideally throw an exception as it's an invalid request.
        Assert.Throws<Exception>(() => CheckReadFunction(
            SelectedFunction: Function.ReadHoldingRegisters,
            PackageNumber: 0,
            SlaveID: 1,
            Address: 0,
            NumberOfRegisters: 0
            ));
    }

    [Fact]
    public void Test_ReadHoldingRegisters_TooManyRegisters_ThrowsException()
    {
        // Modbus specification for Read Holding Registers (0x03) allows max 125 registers.
        // Requesting more than 125 (e.g., 126) should throw an exception.
        Assert.Throws<Exception>(() => CheckReadFunction(
            SelectedFunction: Function.ReadHoldingRegisters,
            PackageNumber: 0,
            SlaveID: 1,
            Address: 0,
            NumberOfRegisters: 126
            ));
    }

    // Общий функционал

    private void CheckReadFunction(ModbusReadFunction SelectedFunction, UInt16 PackageNumber,
        byte SlaveID, UInt16 Address, UInt16 NumberOfRegisters)
    {
        MessageData Data = new ReadTypeMessage(
            SlaveID,
            Address,
            NumberOfRegisters,
            false
            );

        byte[] BytesArray_Actual = Message.CreateMessage(SelectedFunction, Data);

        byte[] PackageNumberArray = BitConverter.GetBytes(PackageNumber);
        byte[] AddressBytes = ModbusField.Get_Address(Address);
        byte[] NumberOfRegistersBytes = ModbusField.Get_NumberOfRegisters(NumberOfRegisters);

        byte[] BytesArray_Expected = new byte[12];

        BytesArray_Expected[0] = PackageNumberArray[1];
        BytesArray_Expected[1] = PackageNumberArray[0];
        // Modbus ID
        BytesArray_Expected[2] = 0;
        BytesArray_Expected[3] = 0;
        // Количество байт далее (байт SlaveID + байты PDU)
        BytesArray_Expected[4] = 0;
        BytesArray_Expected[5] = 6;
        BytesArray_Expected[6] = SlaveID;
        BytesArray_Expected[7] = SelectedFunction.Number;
        BytesArray_Expected[8] = AddressBytes[1];
        BytesArray_Expected[9] = AddressBytes[0];
        BytesArray_Expected[10] = NumberOfRegistersBytes[1];
        BytesArray_Expected[11] = NumberOfRegistersBytes[0];

        Assert.Equal(BytesArray_Actual, BytesArray_Expected);
    }

    private void CheckSingleWriteFunction(ModbusWriteFunction SelectedFunction, UInt16 PackageNumber,
        byte SlaveID, UInt16 Address, UInt16 WriteData)
    {
        UInt16[] WriteDataArray = new UInt16[] { WriteData };

        byte[] bytes = BitConverter.GetBytes(WriteData);

        MessageData Data = new WriteTypeMessage(
            SlaveID,
            Address,
            bytes,
            1,
            false
            );

        byte[] BytesArray_Actual = Message.CreateMessage(SelectedFunction, Data);

        byte[] PackageNumberArray = BitConverter.GetBytes(PackageNumber);
        byte[] AddressBytes = ModbusField.Get_Address(Address);
        byte[] WriteDataBytes = ModbusField.Get_WriteData(WriteDataArray);

        if (WriteDataBytes.Length != 2)
        {
            throw new Exception("При записи одного регистра поле данных должно содержать только 2 байта.");
        }

        byte[] BytesArray_Expected = new byte[12];

        BytesArray_Expected[0] = PackageNumberArray[1];
        BytesArray_Expected[1] = PackageNumberArray[0];
        // Modbus ID
        BytesArray_Expected[2] = 0;
        BytesArray_Expected[3] = 0;
        // Количество байт далее (байт SlaveID + байты PDU)
        BytesArray_Expected[4] = 0;
        BytesArray_Expected[5] = 6;
        BytesArray_Expected[6] = SlaveID;
        BytesArray_Expected[7] = SelectedFunction.Number;
        BytesArray_Expected[8] = AddressBytes[1];
        BytesArray_Expected[9] = AddressBytes[0];
        BytesArray_Expected[10] = WriteDataBytes[0];
        BytesArray_Expected[11] = WriteDataBytes[1];

        Assert.Equal(BytesArray_Expected, BytesArray_Actual);
    }

    private void CheckMultiplyWriteCoilsFunction(UInt16 packageNumber, byte slaveID, UInt16 address, int[] bitArray)
    {
        ModbusWriteFunction selectedFunction = Function.ForceMultipleCoils;

        (byte[] writeBytes, int numberOfCoils) = ModbusField.Get_WriteDataFromMultipleCoils(bitArray);

        MessageData data = new WriteTypeMessage(
            slaveID,
            address,
            writeBytes,
            numberOfCoils,
            false
            );

        byte[] bytesArray_Actual = Message.CreateMessage(selectedFunction, data);

        byte[] bytesArray_Expected = new byte[13 + writeBytes.Length];

        // PDU - 6 байт + байт SlaveID + байты данных
        byte[] slaveID_PDU_Size_Bytes = BitConverter.GetBytes((UInt16)(7 + writeBytes.Length));

        byte[] packageNumberArray = BitConverter.GetBytes(packageNumber);
        byte[] addressBytes = ModbusField.Get_Address(address);
        byte[] numberOfRegisters = ModbusField.Get_NumberOfRegisters((UInt16)numberOfCoils);

        bytesArray_Expected[0] = packageNumberArray[1];
        bytesArray_Expected[1] = packageNumberArray[0];
        // Modbus ID
        bytesArray_Expected[2] = 0;
        bytesArray_Expected[3] = 0;
        // Количество байт далее (байт SlaveID + байты PDU)
        bytesArray_Expected[4] = slaveID_PDU_Size_Bytes[1];
        bytesArray_Expected[5] = slaveID_PDU_Size_Bytes[0];
        bytesArray_Expected[6] = slaveID;
        bytesArray_Expected[7] = selectedFunction.Number;
        bytesArray_Expected[8] = addressBytes[1];
        bytesArray_Expected[9] = addressBytes[0];
        bytesArray_Expected[10] = numberOfRegisters[1];
        bytesArray_Expected[11] = numberOfRegisters[0];
        bytesArray_Expected[12] = (byte)writeBytes.Length;

        Array.Copy(writeBytes, 0, bytesArray_Expected, 13, writeBytes.Length);

        Assert.Equal(bytesArray_Expected, bytesArray_Actual);
    }

    private void CheckMultiplyWriteRegistersFunction(UInt16 PackageNumber, byte SlaveID, UInt16 Address, UInt16[] WriteData)
    {
        ModbusWriteFunction selectedFunction = Function.PresetMultipleRegisters;

        byte[] bytes = WriteData.SelectMany(BitConverter.GetBytes).ToArray();

        MessageData Data = new WriteTypeMessage(
            SlaveID,
            Address,
            bytes,
            WriteData.Length,
            false
            );

        byte[] BytesArray_Actual = Message.CreateMessage(selectedFunction, Data);

        byte[] PackageNumberArray = BitConverter.GetBytes(PackageNumber);
        byte[] AddressBytes = ModbusField.Get_Address(Address);
        byte[] NumberOfRegisters = ModbusField.Get_NumberOfRegisters((UInt16)WriteData.Length);
        byte[] WriteDataBytes = ModbusField.Get_WriteData(WriteData);

        // PDU - 6 байт + байт SlaveID + байты данных
        byte[] SlaveID_PDU_Size_Bytes = BitConverter.GetBytes((UInt16)(7 + WriteDataBytes.Length));

        if (WriteDataBytes.Length != WriteData.Length * 2)
        {
            throw new Exception("Неправильное количество байт в поле данных.");
        }

        byte[] BytesArray_Expected = new byte[13 + WriteDataBytes.Length];

        BytesArray_Expected[0] = PackageNumberArray[1];
        BytesArray_Expected[1] = PackageNumberArray[0];
        // Modbus ID
        BytesArray_Expected[2] = 0;
        BytesArray_Expected[3] = 0;
        // Количество байт далее (байт SlaveID + байты PDU)
        BytesArray_Expected[4] = SlaveID_PDU_Size_Bytes[1];
        BytesArray_Expected[5] = SlaveID_PDU_Size_Bytes[0];
        BytesArray_Expected[6] = SlaveID;
        BytesArray_Expected[7] = selectedFunction.Number;
        BytesArray_Expected[8] = AddressBytes[1];
        BytesArray_Expected[9] = AddressBytes[0];
        BytesArray_Expected[10] = NumberOfRegisters[1];
        BytesArray_Expected[11] = NumberOfRegisters[0];
        BytesArray_Expected[12] = (byte)WriteDataBytes.Length;

        Array.Copy(WriteDataBytes, 0, BytesArray_Expected, 13, WriteDataBytes.Length);

        Assert.Equal(BytesArray_Expected, BytesArray_Actual);
    }
}
