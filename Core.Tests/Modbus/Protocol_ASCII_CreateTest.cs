using Core.Models.Modbus;
using Core.Models.Modbus.DataTypes;
using Core.Models.Modbus.Message;

namespace Core.Tests.Modbus;

public class Protocol_ASCII_CreateTest : BaseProtocolCreateTest
{
    protected override ModbusMessage GetModbusMessageInstance()
    {
        return new ModbusASCII_Message();
    }

    [Fact]
    public void Test_ReadCoilStatus_WithCheckSumEnabled_CreatesCorrectMessage()
    {
        // Ожидаемое Modbus ASCII сообщение (ADU):
        // Начало:            : (0x3A)
        // ID Устройства:     9C (156)
        // Функция:           01 (Чтение статуса катушек)
        // Адрес:             00 0C (12)
        // Количество:        00 05 (5 катушек)
        // LRC:               57
        // Конец:             <CR><LF> (0x0D 0x0A)
        // Полное сообщение:  :9C01000C000557<CR><LF>
        CheckReadFunction(
            selectedFunction: Function.ReadCoilStatus,
            slaveID: 156,
            address: 12,
            numberOfRegisters: 5,
            checkSum_IsEnable: true
            );
    }

    [Fact]
    public void Test_ReadDiscreteInputs_WithCheckSumDisabled_CreatesCorrectMessage()
    {
        // Ожидаемое Modbus ASCII сообщение (ADU):
        // Начало:            : (0x3A)
        // ID Устройства:     2E (46)
        // Функция:           02 (Чтение дискретных входов)
        // Адрес:             00 3B (59)
        // Количество:        00 04 (4 входа)
        // LRC:               (LRC не используется, так как контрольная сумма отключена)
        // Конец:             <CR><LF> (0x0D 0x0A)
        // Полное сообщение:  :2E02003B0004<CR><LF>
        CheckReadFunction(
            selectedFunction: Function.ReadDiscreteInputs,
            slaveID: 46,
            address: 59,
            numberOfRegisters: 4,
            checkSum_IsEnable: false
            );
    }

    [Fact]
    public void Test_ReadHoldingRegisters_WithCheckSumEnabled_CreatesCorrectMessage()
    {
        // Ожидаемое Modbus ASCII сообщение (ADU):
        // Начало:            : (0x3A)
        // ID Устройства:     01 (1)
        // Функция:           03 (Чтение регистров хранения)
        // Адрес:             00 56 (86)
        // Количество:        00 01 (1 регистр)
        // LRC:               A5
        // Конец:             <CR><LF> (0x0D 0x0A)
        // Полное сообщение:  :010300560001A5<CR><LF>
        CheckReadFunction(
            selectedFunction: Function.ReadHoldingRegisters,
            slaveID: 1,
            address: 86,
            numberOfRegisters: 1,
            checkSum_IsEnable: true
            );
    }

    [Fact]
    public void Test_ReadInputRegisters_WithCheckSumDisabled_CreatesCorrectMessage()
    {
        // Ожидаемое Modbus ASCII сообщение (ADU):
        // Начало:            : (0x3A)
        // ID Устройства:     22 (34)
        // Функция:           04 (Чтение входных регистров)
        // Адрес:             00 02 (2)
        // Количество:        00 03 (3 регистра)
        // LRC:               (LRC не используется, так как контрольная сумма отключена)
        // Конец:             <CR><LF> (0x0D 0x0A)
        // Полное сообщение:  :220400020003<CR><LF>
        CheckReadFunction(
            selectedFunction: Function.ReadInputRegisters,
            slaveID: 34,
            address: 2,
            numberOfRegisters: 3,
            checkSum_IsEnable: false
            );
    }

    [Fact]
    public void Test_ForceSingleCoil_WithCheckSumEnabled_CreatesCorrectMessage()
    {
        // Ожидаемое Modbus ASCII сообщение (ADU):
        // Начало:            : (0x3A)
        // ID Устройства:     0D (13)
        // Функция:           05 (Принудительная установка одной катушки)
        // Адрес:             00 60 (96)
        // Данные для записи: FF 00 (ВКЛ)
        // LRC:               C1
        // Конец:             <CR><LF> (0x0D 0x0A)
        // Полное сообщение:  :0D050060FF00C1<CR><LF>
        CheckSingleWriteFunction(
            selectedFunction: Function.ForceSingleCoil,
            slaveID: 13,
            address: 96,
            writeData: 0xFF00,
            checkSum_IsEnable: true
            );
    }

    [Fact]
    public void Test_PresetSingleRegister_WithCheckSumDisabled_CreatesCorrectMessage()
    {
        // Ожидаемое Modbus ASCII сообщение (ADU):
        // Начало:            : (0x3A)
        // ID Устройства:     12 (18)
        // Функция:           06 (Предустановка одного регистра)
        // Адрес:             00 3F (63)
        // Данные для записи: AE D5 (44757)
        // LRC:               (LRC не используется, так как контрольная сумма отключена)
        // Конец:             <CR><LF> (0x0D 0x0A)
        // Полное сообщение:  :1206003FAED5<CR><LF>
        CheckSingleWriteFunction(
            selectedFunction: Function.PresetSingleRegister,
            slaveID: 18,
            address: 63,
            writeData: 0xAED5,
            checkSum_IsEnable: false
            );
    }

    [Fact]
    public void Test_ForceMultipleCoils_WithCheckSumEnabled_CreatesCorrectMessage()
    {
        // Ожидаемое Modbus ASCII сообщение (ADU):
        // Начало:            : (0x3A)
        // ID Устройства:     56 (86)
        // Функция:           0F (Принудительная установка нескольких катушек)
        // Адрес:             00 17 (23)
        // Количество:        00 09 (9 катушек)
        // Количество байт:   02 (2 байта данных катушек)
        // Данные катушек:    AD 03 (1010 1101, 0000 0011 -> катушки 1,3,4,5,7,9 ВКЛ)
        // LRC:               25
        // Конец:             <CR><LF> (0x0D 0x0A)
        // Полное сообщение:  :560F0017000902AD0325<CR><LF>
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
        // Ожидаемое Modbus ASCII сообщение (ADU):
        // Начало:            : (0x3A)
        // ID Устройства:     9C (156)
        // Функция:           10 (Предустановка нескольких регистров)
        // Адрес:             00 56 (86)
        // Количество:        00 05 (5 регистров)
        // Количество байт:   0A (10 байт данных регистров)
        // Данные регистров:  00 FF 45 21 85 00 00 58 DA F8
        // LRC:               A0
        // Конец:             <CR><LF> (0x0D 0x0A)
        // Полное сообщение:  :9C10005600050A00FF452185000058DAF8A0<CR><LF>
        CheckMultiplyWriteRegistersFunction(
            slaveID: 156,
            address: 86,
            writeData: new UInt16[] { 0x00FF, 0x4521, 0x8500, 0x0058, 0xDAF8 },
            checkSum_IsEnable: true
            );
    }

    protected override byte[] CreateExpectedReadMessage(byte slaveID, ModbusReadFunction selectedFunction, UInt16 address, UInt16 numberOfRegisters, bool checkSum_IsEnable)
    {
        byte[] addressBytes = ModbusField.Get_Address(address);
        byte[] numberOfRegistersBytes = ModbusField.Get_NumberOfRegisters(numberOfRegisters);

        byte[] dataBytes = new byte[]
        {
            slaveID,
            selectedFunction.Number,
            addressBytes[1],
            addressBytes[0],
            numberOfRegistersBytes[1],
            numberOfRegistersBytes[0]
        };

        return Create_ASCII_Package(dataBytes, checkSum_IsEnable);
    }

    protected override byte[] CreateExpectedSingleWriteMessage(byte slaveID, ModbusWriteFunction selectedFunction, UInt16 address, UInt16 writeData, bool checkSum_IsEnable)
    {
        UInt16[] writeDataArray = new UInt16[] { writeData };

        byte[] addressBytes = ModbusField.Get_Address(address);
        byte[] writeDataBytes = ModbusField.Get_WriteData(writeDataArray);

        if (writeDataBytes.Length != 2)
        {
            throw new Exception("При записи одного регистра поле данных должно содержать только 2 байта.");
        }

        byte[] dataBytes = new byte[]
        {
            slaveID,
            selectedFunction.Number,
            addressBytes[1],
            addressBytes[0],
            writeDataBytes[0],
            writeDataBytes[1]
        };

        return Create_ASCII_Package(dataBytes, checkSum_IsEnable);
    }

    protected override byte[] CreateExpectedMultiplyWriteCoilsMessage(byte slaveID, ModbusWriteFunction selectedFunction, UInt16 address, int[] bitArray, bool checkSum_IsEnable)
    {
        (byte[] writeBytes, int numberOfCoils) = ModbusField.Get_WriteDataFromMultipleCoils(bitArray);

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

        return Create_ASCII_Package(dataBytes, checkSum_IsEnable);
    }

    protected override byte[] CreateExpectedMultiplyWriteRegistersMessage(byte slaveID, ModbusWriteFunction selectedFunction, UInt16 address, UInt16[] writeData, bool checkSum_IsEnable)
    {
        byte[] addressBytes = ModbusField.Get_Address(address);
        byte[] numberOfRegisters = ModbusField.Get_NumberOfRegisters((UInt16)writeData.Length);
        byte[] writeDataBytes = ModbusField.Get_WriteData(writeData);

        if (writeDataBytes.Length != writeData.Length * 2)
        {
            throw new Exception("Неправильное количество байт в поле данных.");
        }

        byte[] dataBytes = new byte[7 + writeDataBytes.Length];

        dataBytes[0] = slaveID;
        dataBytes[1] = selectedFunction.Number;
        dataBytes[2] = addressBytes[1];
        dataBytes[3] = addressBytes[0];
        dataBytes[4] = numberOfRegisters[1];
        dataBytes[5] = numberOfRegisters[0];
        dataBytes[6] = (byte)writeDataBytes.Length;

        Array.Copy(writeDataBytes, 0, dataBytes, 7, writeDataBytes.Length);

        return Create_ASCII_Package(dataBytes, checkSum_IsEnable);
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
