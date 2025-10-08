using Core.Models.Modbus;
using Core.Models.Modbus.DataTypes;
using Core.Models.Modbus.Message;

namespace Core.Tests.Modbus;

public class Protocol_ASCII_CreateTest
{
    private ModbusMessage Message = new ModbusASCII_Message();

    [Fact]
    public void Test_ReadCoilStatus_WithCheckSumEnabled_CreatesCorrectMessage()
    {
        // Expected Modbus ASCII message:
        // :9C01000C000557<CR><LF>
        CheckReadFunction(
            SelectedFunction: Function.ReadCoilStatus,
            SlaveID: 156,
            Address: 12,
            NumberOfRegisters: 5,
            CheckSum_IsEnable: true
            );
    }

    [Fact]
    public void Test_ReadDiscreteInputs_WithCheckSumDisabled_CreatesCorrectMessage()
    {
        // Expected Modbus ASCII message:
        // :2E02003B0004<CR><LF> (no LRC, as checksum is disabled)
        CheckReadFunction(
            SelectedFunction: Function.ReadDiscreteInputs,
            SlaveID: 46,
            Address: 59,
            NumberOfRegisters: 4,
            CheckSum_IsEnable: false
            );
    }

    [Fact]
    public void Test_ReadHoldingRegisters_WithCheckSumEnabled_CreatesCorrectMessage()
    {
        // Expected Modbus ASCII message:
        // :010300560001A5<CR><LF>
        CheckReadFunction(
            SelectedFunction: Function.ReadHoldingRegisters,
            SlaveID: 1,
            Address: 86,
            NumberOfRegisters: 1,
            CheckSum_IsEnable: true
            );
    }

    [Fact]
    public void Test_ReadInputRegisters_WithCheckSumDisabled_CreatesCorrectMessage()
    {
        // Expected Modbus ASCII message:
        // :220400020003<CR><LF> (no LRC, as checksum is disabled)
        CheckReadFunction(
            SelectedFunction: Function.ReadInputRegisters,
            SlaveID: 34,
            Address: 2,
            NumberOfRegisters: 3,
            CheckSum_IsEnable: false
            );
    }

    [Fact]
    public void Test_ForceSingleCoil_WithCheckSumEnabled_CreatesCorrectMessage()
    {
        // Expected Modbus ASCII message:
        // :0D050060FF00C1<CR><LF>
        CheckSingleWriteFunction(
            SelectedFunction: Function.ForceSingleCoil,
            SlaveID: 13,
            Address: 96,
            WriteData: 0xFF00,
            CheckSum_IsEnable: true
            );
    }

    [Fact]
    public void Test_PresetSingleRegister_WithCheckSumDisabled_CreatesCorrectMessage()
    {
        // Expected Modbus ASCII message:
        // :1206003FAED5<CR><LF> (no LRC, as checksum is disabled)
        CheckSingleWriteFunction(
            SelectedFunction: Function.PresetSingleRegister,
            SlaveID: 18,
            Address: 63,
            WriteData: 0xAED5,
            CheckSum_IsEnable: false
            );
    }

    [Fact]
    public void Test_ForceMultipleCoils_WithCheckSumEnabled_CreatesCorrectMessage()
    {
        // Expected Modbus ASCII message:
        // :560F0017000902AD0325<CR><LF>
        CheckMultiplyWriteCoilsFunction(
            slaveID: 86,
            address: 23,
            bitArray: new int[] { 1, 0, 1, 1, 1, 1, 0, 0, 1 },
            checkSum_IsEnable: true
            );
    }

    [Fact]
    public void Test_PresetMultipleRegisters_WithCheckSumEnabled_CreatesCorrectMessage()
    {
        // Expected Modbus ASCII message:
        // :9C10005600050A00FF452185000058DAF8A0<CR><LF>
        CheckMultiplyWriteRegistersFunction(
            SlaveID: 156,
            Address: 86,
            WriteData: new UInt16[] { 0x00FF, 0x4521, 0x8500, 0x0058, 0xDAF8 },
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
        // Expected Modbus ASCII message for Address 0xFFFF and 1 register (with checksum enabled)
        // :9C03FFFF000109<CR><LF>
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

        byte[] DataBytes = new byte[]
        {
                SlaveID,
                SelectedFunction.Number,
                AddressBytes[1],
                AddressBytes[0],
                NumberOfRegistersBytes[1],
                NumberOfRegistersBytes[0]
        };

        byte[] BytesArray_Expected = Create_ASCII_Package(DataBytes, CheckSum_IsEnable);

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

        byte[] DataBytes = new byte[]
        {
                SlaveID,
                SelectedFunction.Number,
                AddressBytes[1],
                AddressBytes[0],
                WriteDataBytes[0],
                WriteDataBytes[1]
        };

        byte[] BytesArray_Expected = Create_ASCII_Package(DataBytes, CheckSum_IsEnable);

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

        byte[] addressBytes = ModbusField.Get_Address(address);
        byte[] numberOfRegisters = ModbusField.Get_NumberOfRegisters((UInt16)numberOfCoils);

        byte[] dataBytes = new byte[7 + writeBytes.Length];

        dataBytes[0] = slaveID;
        dataBytes[1] = selectedFunction.Number;
        dataBytes[2] = addressBytes[1];
        dataBytes[3] = addressBytes[0];
        dataBytes[4] = numberOfRegisters[1];
        dataBytes[5] = numberOfRegisters[0];
        dataBytes[6] = (byte)writeBytes.Length;

        Array.Copy(writeBytes, 0, dataBytes, 7, writeBytes.Length);

        byte[] bytesArray_Expected = Create_ASCII_Package(dataBytes, checkSum_IsEnable);

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

        byte[] DataBytes = new byte[7 + WriteDataBytes.Length];

        DataBytes[0] = SlaveID;
        DataBytes[1] = selectedFunction.Number;
        DataBytes[2] = AddressBytes[1];
        DataBytes[3] = AddressBytes[0];
        DataBytes[4] = NumberOfRegisters[1];
        DataBytes[5] = NumberOfRegisters[0];
        DataBytes[6] = (byte)WriteDataBytes.Length;

        Array.Copy(WriteDataBytes, 0, DataBytes, 7, WriteDataBytes.Length);

        byte[] BytesArray_Expected = Create_ASCII_Package(DataBytes, CheckSum_IsEnable);

        Assert.Equal(BytesArray_Expected, BytesArray_Actual);
    }

    private byte[] Create_ASCII_Package(byte[] MessageBytes, bool CheckSum_IsEnable)
    {
        byte[] DataBytes_ASCII = ModbusASCII_Message.ConvertArrayToASCII(MessageBytes);

        byte[] ResultArray;

        if (CheckSum_IsEnable)
        {
            ResultArray = new byte[5 + DataBytes_ASCII.Length];
        }

        else
        {
            ResultArray = new byte[3 + DataBytes_ASCII.Length];
        }

        // Символ начала кадра (префикс)
        ResultArray[0] = 0x3A;

        Array.Copy(DataBytes_ASCII, 0, ResultArray, 1, DataBytes_ASCII.Length);

        // LRC8
        if (CheckSum_IsEnable)
        {
            byte[] LRC8 = CheckSum.Calculate_LRC8(MessageBytes);
            ResultArray[ResultArray.Length - 4] = LRC8[0];
            ResultArray[ResultArray.Length - 3] = LRC8[1];
        }

        // Символы конца кадра
        ResultArray[ResultArray.Length - 2] = 0x0D;  // Предпоследний элемент
        ResultArray[ResultArray.Length - 1] = 0x0A;  // Последний элемент


        return ResultArray;
    }
}
