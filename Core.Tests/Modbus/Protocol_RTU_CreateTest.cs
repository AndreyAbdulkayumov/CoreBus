using Core.Models.Modbus;
using Core.Models.Modbus.DataTypes;
using Core.Models.Modbus.Message;

namespace Core.Tests.Modbus;

public class Protocol_RTU_CreateTest
{
    private ModbusMessage Message = new ModbusRTU_Message();

    [Fact]
    public void Test_ReadCoilStatus_WithCheckSumEnabled_CreatesCorrectMessage()
    {
        // Expected Modbus RTU message:
        // 0E 01 00 0C 00 05 10 C8
        CheckReadFunction(
            SelectedFunction: Function.ReadCoilStatus,
            SlaveID: 14,
            Address: 12,
            NumberOfRegisters: 5,
            CheckSum_IsEnable: true
            );
    }

    [Fact]
    public void Test_ReadDiscreteInputs_WithCheckSumDisabled_CreatesCorrectMessage()
    {
        // Expected Modbus RTU message:
        // 63 02 01 00 00 0C (no CRC, as checksum is disabled)
        CheckReadFunction(
            SelectedFunction: Function.ReadDiscreteInputs,
            SlaveID: 99,
            Address: 256,
            NumberOfRegisters: 12,
            CheckSum_IsEnable: false
            );
    }

    [Fact]
    public void Test_ReadHoldingRegisters_WithCheckSumEnabled_CreatesCorrectMessage()
    {
        // Expected Modbus RTU message:
        // 9C 03 00 56 00 01 ED C0
        CheckReadFunction(
            SelectedFunction: Function.ReadHoldingRegisters,
            SlaveID: 156,
            Address: 86,
            NumberOfRegisters: 1,
            CheckSum_IsEnable: true
            );
    }

    [Fact]
    public void Test_ReadInputRegisters_WithCheckSumDisabled_CreatesCorrectMessage()
    {
        // Expected Modbus RTU message:
        // 38 04 01 00 00 09 (no CRC, as checksum is disabled)
        CheckReadFunction(
            SelectedFunction: Function.ReadInputRegisters,
            SlaveID: 56,
            Address: 256,
            NumberOfRegisters: 9,
            CheckSum_IsEnable: false
            );
    }

    [Fact]
    public void Test_ForceSingleCoil_WithCheckSumEnabled_CreatesCorrectMessage()
    {
        // Expected Modbus RTU message:
        // 2D 05 00 52 FF 00 3C C8
        CheckSingleWriteFunction(
            SelectedFunction: Function.ForceSingleCoil,
            SlaveID: 45,
            Address: 82,
            WriteData: 0xFF00,
            CheckSum_IsEnable: true
            );
    }

    [Fact]
    public void Test_PresetSingleRegister_WithCheckSumDisabled_CreatesCorrectMessage()
    {
        // Expected Modbus RTU message:
        // 48 06 02 EF 09 15 (no CRC, as checksum is disabled)
        CheckSingleWriteFunction(
            SelectedFunction: Function.PresetSingleRegister,
            SlaveID: 72,
            Address: 753,
            WriteData: 0x0915,
            CheckSum_IsEnable: false
            );
    }

    [Fact]
    public void Test_ForceMultipleCoils_WithCheckSumEnabled_CreatesCorrectMessage()
    {
        // Expected Modbus RTU message:
        // 0F 0F 00 49 00 08 01 55 C9 5A
        CheckMultiplyWriteCoilsFunction(
            slaveID: 15,
            address: 73,
            bitArray: new int[] { 1, 1, 1, 0, 0, 1, 0, 1 },
            checkSum_IsEnable: true
            );
    }

    [Fact]
    public void Test_PresetMultipleRegisters_WithCheckSumEnabled_CreatesCorrectMessage()
    {
        // Expected Modbus RTU message:
        // F0 10 11 0D 00 03 06 05 45 00 89 75 34 D0 BB
        CheckMultiplyWriteRegistersFunction(
            SlaveID: 240,
            Address: 4365,
            WriteData: new UInt16[] { 0x0545, 0x0089, 0x7534 },
            CheckSum_IsEnable: true
            );
    }

    [Fact]
    public void Test_ReadCoilStatus_InvalidSlaveID_Zero_ThrowsException()
    {
        // SlaveID 0 is typically reserved for broadcast and should not be used as a unit address.
        // Expecting an exception or a specific error handling.
        Assert.Throws<Exception>(() => CheckReadFunction(
            SelectedFunction: Function.ReadCoilStatus,
            SlaveID: 0,
            Address: 12,
            NumberOfRegisters: 5,
            CheckSum_IsEnable: true
            ));
    }

    [Fact]
    public void Test_ReadCoilStatus_InvalidSlaveID_MaxByte_ThrowsException()
    {
        // SlaveID 255 is also an invalid unit address, often used for broadcast in some contexts.
        // Expecting an exception or a specific error handling.
        Assert.Throws<Exception>(() => CheckReadFunction(
            SelectedFunction: Function.ReadCoilStatus,
            SlaveID: 255,
            Address: 12,
            NumberOfRegisters: 5,
            CheckSum_IsEnable: true
            ));
    }

    [Fact]
    public void Test_ReadHoldingRegisters_MaxAddress_OneRegister_CreatesCorrectMessage()
    {
        // Expected Modbus RTU message for Address 0xFFFF and 1 register (with checksum enabled)
        // 9C 03 FF FF 00 01 C3 82
        CheckReadFunction(
            SelectedFunction: Function.ReadHoldingRegisters,
            SlaveID: 156,
            Address: 0xFFFF,
            NumberOfRegisters: 1,
            CheckSum_IsEnable: true
            );
    }

    [Fact]
    public void Test_ReadHoldingRegisters_ZeroRegisters_ThrowsException()
    {
        // NumberOfRegisters = 0 should ideally throw an exception as it's an invalid request.
        Assert.Throws<Exception>(() => CheckReadFunction(
            SelectedFunction: Function.ReadHoldingRegisters,
            SlaveID: 1,
            Address: 0,
            NumberOfRegisters: 0,
            CheckSum_IsEnable: true
            ));
    }

    [Fact]
    public void Test_ReadHoldingRegisters_TooManyRegisters_ThrowsException()
    {
        // Modbus specification for Read Holding Registers (0x03) allows max 125 registers.
        // Requesting more than 125 (e.g., 126) should throw an exception.
        Assert.Throws<Exception>(() => CheckReadFunction(
            SelectedFunction: Function.ReadHoldingRegisters,
            SlaveID: 1,
            Address: 0,
            NumberOfRegisters: 126,
            CheckSum_IsEnable: true
            ));
    }

    // Общий функционал

    private void CheckReadFunction(ModbusReadFunction SelectedFunction,
        byte SlaveID, UInt16 Address, UInt16 NumberOfRegisters, bool CheckSum_IsEnable)
    {
        MessageData Data = new ReadTypeMessage(
            SlaveID,
            Address,
            NumberOfRegisters,
            CheckSum_IsEnable
            );

        byte[] BytesArray_Actual = Message.CreateMessage(SelectedFunction, Data);

        byte[] AddressBytes = ModbusField.Get_Address(Address);
        byte[] NumberOfRegistersBytes = ModbusField.Get_NumberOfRegisters(NumberOfRegisters);

        byte[] BytesArray_Expected;

        if (Data.CheckSum_IsEnable)
        {
            BytesArray_Expected = new byte[8];
        }

        else
        {
            BytesArray_Expected = new byte[6];
        }

        BytesArray_Expected[0] = SlaveID;
        BytesArray_Expected[1] = SelectedFunction.Number;
        BytesArray_Expected[2] = AddressBytes[1];
        BytesArray_Expected[3] = AddressBytes[0];
        BytesArray_Expected[4] = NumberOfRegistersBytes[1];
        BytesArray_Expected[5] = NumberOfRegistersBytes[0];

        if (Data.CheckSum_IsEnable)
        {
            byte[] CheckSumBytes = CheckSum.Calculate_CRC16(BytesArray_Expected);
            BytesArray_Expected[6] = CheckSumBytes[0];
            BytesArray_Expected[7] = CheckSumBytes[1];
        }

        Assert.Equal(BytesArray_Actual, BytesArray_Expected);
    }

    private void CheckSingleWriteFunction(ModbusWriteFunction SelectedFunction,
        byte SlaveID, UInt16 Address, UInt16 WriteData, bool CheckSum_IsEnable)
    {
        UInt16[] WriteDataArray = new UInt16[] { WriteData };

        byte[] bytes = BitConverter.GetBytes(WriteData);

        MessageData Data = new WriteTypeMessage(
            SlaveID,
            Address,
            bytes,
            1,
            CheckSum_IsEnable
            );

        byte[] BytesArray_Actual = Message.CreateMessage(SelectedFunction, Data);

        byte[] AddressBytes = ModbusField.Get_Address(Address);
        byte[] WriteDataBytes = ModbusField.Get_WriteData(WriteDataArray);

        if (WriteDataBytes.Length != 2)
        {
            throw new Exception("При записи одного регистра поле данных должно содержать только 2 байта.");
        }

        byte[] BytesArray_Expected;

        if (Data.CheckSum_IsEnable)
        {
            BytesArray_Expected = new byte[8];
        }

        else
        {
            BytesArray_Expected = new byte[6];
        }

        BytesArray_Expected[0] = SlaveID;
        BytesArray_Expected[1] = SelectedFunction.Number;
        BytesArray_Expected[2] = AddressBytes[1];
        BytesArray_Expected[3] = AddressBytes[0];
        BytesArray_Expected[4] = WriteDataBytes[0];
        BytesArray_Expected[5] = WriteDataBytes[1];

        if (Data.CheckSum_IsEnable)
        {
            byte[] CheckSumBytes = CheckSum.Calculate_CRC16(BytesArray_Expected);
            BytesArray_Expected[6] = CheckSumBytes[0];
            BytesArray_Expected[7] = CheckSumBytes[1];
        }

        Assert.Equal(BytesArray_Expected, BytesArray_Actual);
    }

    private void CheckMultiplyWriteCoilsFunction(byte slaveID, UInt16 address, int[] bitArray, bool checkSum_IsEnable)
    {
        ModbusWriteFunction selectedFunction = Function.ForceMultipleCoils;

        (byte[] writeBytes, int numberOfCoils) = ModbusField.Get_WriteDataFromMultipleCoils(bitArray);

        MessageData data = new WriteTypeMessage(
            slaveID,
            address,
            writeBytes,
            numberOfCoils,
            checkSum_IsEnable
            );

        byte[] bytesArray_Actual = Message.CreateMessage(selectedFunction, data);

        byte[] bytesArray_Expected = checkSum_IsEnable ? new byte[9 + writeBytes.Length] : new byte[7 + writeBytes.Length];

        byte[] addressBytes = ModbusField.Get_Address(address);
        byte[] numberOfRegisters = ModbusField.Get_NumberOfRegisters((UInt16)numberOfCoils);

        bytesArray_Expected[0] = slaveID;
        bytesArray_Expected[1] = selectedFunction.Number;
        bytesArray_Expected[2] = addressBytes[1];
        bytesArray_Expected[3] = addressBytes[0];
        bytesArray_Expected[4] = numberOfRegisters[1];
        bytesArray_Expected[5] = numberOfRegisters[0];
        bytesArray_Expected[6] = (byte)writeBytes.Length;

        Array.Copy(writeBytes, 0, bytesArray_Expected, 7, writeBytes.Length);

        if (checkSum_IsEnable)
        {
            byte[] checkSumBytes = CheckSum.Calculate_CRC16(bytesArray_Expected);
            bytesArray_Expected[bytesArray_Expected.Length - 2] = checkSumBytes[0];
            bytesArray_Expected[bytesArray_Expected.Length - 1] = checkSumBytes[1];
        }

        Assert.Equal(bytesArray_Expected, bytesArray_Actual);
    }

    private void CheckMultiplyWriteRegistersFunction(byte SlaveID, UInt16 Address, UInt16[] WriteData, bool CheckSum_IsEnable)
    {
        ModbusWriteFunction selectedFunction = Function.PresetMultipleRegisters;

        byte[] bytes = WriteData.SelectMany(BitConverter.GetBytes).ToArray();

        MessageData Data = new WriteTypeMessage(
            SlaveID,
            Address,
            bytes,
            WriteData.Length,
            CheckSum_IsEnable
            );

        byte[] BytesArray_Actual = Message.CreateMessage(selectedFunction, Data);

        byte[] AddressBytes = ModbusField.Get_Address(Address);
        byte[] NumberOfRegisters = ModbusField.Get_NumberOfRegisters((UInt16)WriteData.Length);
        byte[] WriteDataBytes = ModbusField.Get_WriteData(WriteData);

        if (WriteDataBytes.Length != WriteData.Length * 2)
        {
            throw new Exception("Неправильное количество байт в поле данных.");
        }

        byte[] BytesArray_Expected;

        if (Data.CheckSum_IsEnable)
        {
            BytesArray_Expected = new byte[9 + WriteDataBytes.Length];
        }

        else
        {
            BytesArray_Expected = new byte[7 + WriteDataBytes.Length];
        }

        BytesArray_Expected[0] = SlaveID;
        BytesArray_Expected[1] = selectedFunction.Number;
        BytesArray_Expected[2] = AddressBytes[1];
        BytesArray_Expected[3] = AddressBytes[0];
        BytesArray_Expected[4] = NumberOfRegisters[1];
        BytesArray_Expected[5] = NumberOfRegisters[0];
        BytesArray_Expected[6] = (byte)WriteDataBytes.Length;

        Array.Copy(WriteDataBytes, 0, BytesArray_Expected, 7, WriteDataBytes.Length);

        if (Data.CheckSum_IsEnable)
        {
            byte[] CheckSumBytes = CheckSum.Calculate_CRC16(BytesArray_Expected);
            BytesArray_Expected[BytesArray_Expected.Length - 2] = CheckSumBytes[0];
            BytesArray_Expected[BytesArray_Expected.Length - 1] = CheckSumBytes[1];
        }

        Assert.Equal(BytesArray_Expected, BytesArray_Actual);
    }
}
