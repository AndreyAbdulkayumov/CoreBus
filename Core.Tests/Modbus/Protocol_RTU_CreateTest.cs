using Core.Models.Modbus;
using Core.Models.Modbus.DataTypes;
using Core.Models.Modbus.Message;

namespace Core.Tests.Modbus;

public class Protocol_RTU_CreateTest : BaseProtocolCreateTest
{
    protected override ModbusMessage GetModbusMessageInstance()
    {
        return new ModbusRTU_Message();
    }

    [Fact]
    public void Test_ReadCoilStatus_WithCheckSumEnabled_CreatesCorrectMessage()
    {
        // Ожидаемое Modbus RTU сообщение (ADU):
        // ID Устройства:     0E (14)
        // Функция:           01 (Чтение статуса катушек)
        // Адрес:             00 0C (12)
        // Количество:        00 05 (5 катушек)
        // CRC:               10 C8
        // Полное сообщение:  0E 01 00 0C 00 05 10 C8
        CheckReadFunction(
            selectedFunction: Function.ReadCoilStatus,
            slaveID: 14,
            address: 12,
            numberOfRegisters: 5,
            checkSum_IsEnable: true
            );
    }

    [Fact]
    public void Test_ReadDiscreteInputs_WithCheckSumDisabled_CreatesCorrectMessage()
    {
        // Ожидаемое Modbus RTU сообщение (ADU):
        // ID Устройства:     63 (99)
        // Функция:           02 (Чтение дискретных входов)
        // Адрес:             01 00 (256)
        // Количество:        00 0C (12 входов)
        // CRC:               (CRC не используется, так как контрольная сумма отключена)
        // Полное сообщение:  63 02 01 00 00 0C
        CheckReadFunction(
            selectedFunction: Function.ReadDiscreteInputs,
            slaveID: 99,
            address: 256,
            numberOfRegisters: 12,
            checkSum_IsEnable: false
            );
    }

    [Fact]
    public void Test_ReadHoldingRegisters_WithCheckSumEnabled_CreatesCorrectMessage()
    {
        // Ожидаемое Modbus RTU сообщение (ADU):
        // ID Устройства:     9C (156)
        // Функция:           03 (Чтение регистров хранения)
        // Адрес:             00 56 (86)
        // Количество:        00 01 (1 регистр)
        // CRC:               ED C0
        // Полное сообщение:  9C 03 00 56 00 01 ED C0
        CheckReadFunction(
            selectedFunction: Function.ReadHoldingRegisters,
            slaveID: 156,
            address: 86,
            numberOfRegisters: 1,
            checkSum_IsEnable: true
            );
    }

    [Fact]
    public void Test_ReadInputRegisters_WithCheckSumDisabled_CreatesCorrectMessage()
    {
        // Ожидаемое Modbus RTU сообщение (ADU):
        // ID Устройства:     38 (56)
        // Функция:           04 (Чтение входных регистров)
        // Адрес:             01 00 (256)
        // Количество:        00 09 (9 регистров)
        // CRC:               (CRC не используется, так как контрольная сумма отключена)
        // Полное сообщение:  38 04 01 00 00 09
        CheckReadFunction(
            selectedFunction: Function.ReadInputRegisters,
            slaveID: 56,
            address: 256,
            numberOfRegisters: 9,
            checkSum_IsEnable: false
            );
    }

    [Fact]
    public void Test_ForceSingleCoil_WithCheckSumEnabled_CreatesCorrectMessage()
    {
        // Ожидаемое Modbus RTU сообщение (ADU):
        // ID Устройства:     2D (45)
        // Функция:           05 (Принудительная установка одной катушки)
        // Адрес:             00 52 (82)
        // Данные для записи: FF 00 (ВКЛ)
        // CRC:               3C C8
        // Полное сообщение:  2D 05 00 52 FF 00 3C C8
        CheckSingleWriteFunction(
            selectedFunction: Function.ForceSingleCoil,
            slaveID: 45,
            address: 82,
            writeData: 0xFF00,
            checkSum_IsEnable: true
            );
    }

    [Fact]
    public void Test_PresetSingleRegister_WithCheckSumDisabled_CreatesCorrectMessage()
    {
        // Ожидаемое Modbus RTU сообщение (ADU):
        // ID Устройства:     48 (72)
        // Функция:           06 (Предустановка одного регистра)
        // Адрес:             02 EF (753)
        // Данные для записи: 09 15 (2325)
        // CRC:               (CRC не используется, так как контрольная сумма отключена)
        // Полное сообщение:  48 06 02 EF 09 15
        CheckSingleWriteFunction(
            selectedFunction: Function.PresetSingleRegister,
            slaveID: 72,
            address: 753,
            writeData: 0x0915,
            checkSum_IsEnable: false
            );
    }

    [Fact]
    public void Test_ForceMultipleCoils_WithCheckSumEnabled_CreatesCorrectMessage()
    {
        // Ожидаемое Modbus RTU сообщение (ADU):
        // ID Устройства:     0F (15)
        // Функция:           0F (Принудительная установка нескольких катушек)
        // Адрес:             00 49 (73)
        // Количество:        00 08 (8 катушек)
        // Количество байт:   01 (1 байт данных катушек)
        // Данные катушек:    55 (0101 0101 -> катушки 1,3,5,7 ВКЛ)
        // CRC:               C9 5A
        // Полное сообщение:  0F 0F 00 49 00 08 01 55 C9 5A
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
        // Ожидаемое Modbus RTU сообщение (ADU):
        // ID Устройства:     F0 (240)
        // Функция:           10 (Предустановка нескольких регистров)
        // Адрес:             11 0D (4365)
        // Количество:        00 03 (3 регистра)
        // Количество байт:   06 (6 байт данных регистров)
        // Данные регистров:  05 45 00 89 75 34
        // CRC:               D0 BB
        // Полное сообщение:  F0 10 11 0D 00 03 06 05 45 00 89 75 34 D0 BB
        CheckMultiplyWriteRegistersFunction(
            slaveID: 240,
            address: 4365,
            writeData: new UInt16[] { 0x0545, 0x0089, 0x7534 },
            checkSum_IsEnable: true
            );
    }

    protected override byte[] CreateExpectedReadMessage(byte slaveID, ModbusReadFunction selectedFunction, UInt16 address, UInt16 numberOfRegisters, bool checkSum_IsEnable)
    {
        byte[] addressBytes = ModbusField.Get_Address(address);
        byte[] numberOfRegistersBytes = ModbusField.Get_NumberOfRegisters(numberOfRegisters);

        byte[] bytesArray_Expected;

        if (checkSum_IsEnable)
        {
            bytesArray_Expected = new byte[8];
        }

        else
        {
            bytesArray_Expected = new byte[6];
        }

        bytesArray_Expected[0] = slaveID;
        bytesArray_Expected[1] = selectedFunction.Number;
        bytesArray_Expected[2] = addressBytes[1];
        bytesArray_Expected[3] = addressBytes[0];
        bytesArray_Expected[4] = numberOfRegistersBytes[1];
        bytesArray_Expected[5] = numberOfRegistersBytes[0];

        if (checkSum_IsEnable)
        {
            byte[] checkSumBytes = CheckSum.Calculate_CRC16(bytesArray_Expected, 0xA001);
            bytesArray_Expected[6] = checkSumBytes[0];
            bytesArray_Expected[7] = checkSumBytes[1];
        }

        return bytesArray_Expected;
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

        byte[] bytesArray_Expected;

        if (checkSum_IsEnable)
        {
            bytesArray_Expected = new byte[8];
        }

        else
        {
            bytesArray_Expected = new byte[6];
        }

        bytesArray_Expected[0] = slaveID;
        bytesArray_Expected[1] = selectedFunction.Number;
        bytesArray_Expected[2] = addressBytes[1];
        bytesArray_Expected[3] = addressBytes[0];
        bytesArray_Expected[4] = writeDataBytes[0];
        bytesArray_Expected[5] = writeDataBytes[1];

        if (checkSum_IsEnable)
        {
            byte[] checkSumBytes = CheckSum.Calculate_CRC16(bytesArray_Expected, 0xA001);
            bytesArray_Expected[6] = checkSumBytes[0];
            bytesArray_Expected[7] = checkSumBytes[1];
        }

        return bytesArray_Expected;
    }

    protected override byte[] CreateExpectedMultiplyWriteCoilsMessage(byte slaveID, ModbusWriteFunction selectedFunction, UInt16 address, int[] bitArray, bool checkSum_IsEnable)
    {
        (byte[] writeBytes, int numberOfCoils) = ModbusField.Get_WriteDataFromMultipleCoils(bitArray);

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
            byte[] checkSumBytes = CheckSum.Calculate_CRC16(bytesArray_Expected, 0xA001);
            bytesArray_Expected[bytesArray_Expected.Length - 2] = checkSumBytes[0];
            bytesArray_Expected[bytesArray_Expected.Length - 1] = checkSumBytes[1];
        }

        return bytesArray_Expected;
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

        byte[] bytesArray_Expected;

        if (checkSum_IsEnable)
        {
            bytesArray_Expected = new byte[9 + writeDataBytes.Length];
        }

        else
        {
            bytesArray_Expected = new byte[7 + writeDataBytes.Length];
        }

        bytesArray_Expected[0] = slaveID;
        bytesArray_Expected[1] = selectedFunction.Number;
        bytesArray_Expected[2] = addressBytes[1];
        bytesArray_Expected[3] = addressBytes[0];
        bytesArray_Expected[4] = numberOfRegisters[1];
        bytesArray_Expected[5] = numberOfRegisters[0];
        bytesArray_Expected[6] = (byte)writeDataBytes.Length;

        Array.Copy(writeDataBytes, 0, bytesArray_Expected, 7, writeDataBytes.Length);

        if (checkSum_IsEnable)
        {
            byte[] checkSumBytes = CheckSum.Calculate_CRC16(bytesArray_Expected, 0xA001);
            bytesArray_Expected[bytesArray_Expected.Length - 2] = checkSumBytes[0];
            bytesArray_Expected[bytesArray_Expected.Length - 1] = checkSumBytes[1];
        }

        return bytesArray_Expected;
    }
}
