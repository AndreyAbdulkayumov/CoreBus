using Core.Models.Modbus.DataTypes;
using Core.Models.Modbus.Message;

namespace Core.Tests.Modbus;

public class Protocol_TCP_CreateTest : BaseProtocolCreateTest
{
    protected override ModbusMessage GetModbusMessageInstance()
    {
        return new ModbusTCP_Message();
    }

    // PackageNumber делать всегда равным 0

    [Fact]
    public void Test_ReadCoilStatus_CreatesCorrectMessage()
    {
        // Ожидаемое Modbus TCP сообщение (Заголовок MBAP + PDU):
        // ID Транзакции:     00 00
        // ID Протокола:      00 00
        // Длина:             00 06 (ID Устройства + длина PDU)
        // ID Устройства:     9C (156)
        // PDU:               01 00 0C 00 05 (Код функции 01, Адрес 000C, Количество 0005)
        // Полное сообщение:  00 00 00 00 00 06 9C 01 00 0C 00 05
        CheckReadFunction(
            selectedFunction: Function.ReadCoilStatus,
            packageNumber: 0,
            slaveID: 156,
            address: 12,
            numberOfRegisters: 5
            );
    }

    [Fact]
    public void Test_ReadDiscreteInputs_CreatesCorrectMessage()
    {
        // Ожидаемое Modbus TCP сообщение (Заголовок MBAP + PDU):
        // ID Транзакции:     00 00
        // ID Протокола:      00 00
        // Длина:             00 06 (ID Устройства + длина PDU)
        // ID Устройства:     AC (172)
        // PDU:               02 00 0C 00 02 (Код функции 02, Адрес 000C, Количество 0002)
        // Полное сообщение:  00 00 00 00 00 06 AC 02 00 0C 00 02
        CheckReadFunction(
            selectedFunction: Function.ReadDiscreteInputs,
            packageNumber: 0,
            slaveID: 172,
            address: 12,
            numberOfRegisters: 2
            );
    }

    [Fact]
    public void Test_ReadHoldingRegisters_CreatesCorrectMessage()
    {
        // Ожидаемое Modbus TCP сообщение (Заголовок MBAP + PDU):
        // ID Транзакции:     00 00
        // ID Протокола:      00 00
        // Длина:             00 06 (ID Устройства + длина PDU)
        // ID Устройства:     10 (16)
        // PDU:               03 00 2A 00 05 (Код функции 03, Адрес 002A, Количество 0005)
        // Полное сообщение:  00 00 00 00 00 06 10 03 00 2A 00 05
        CheckReadFunction(
            selectedFunction: Function.ReadHoldingRegisters,
            packageNumber: 0,
            slaveID: 16,
            address: 42,
            numberOfRegisters: 5
            );
    }

    [Fact]
    public void Test_ReadInputRegisters_CreatesCorrectMessage()
    {
        // Ожидаемое Modbus TCP сообщение (Заголовок MBAP + PDU):
        // ID Транзакции:     00 00
        // ID Протокола:      00 00
        // Длина:             00 06 (ID Устройства + длина PDU)
        // ID Устройства:     06 (6)
        // PDU:               04 00 13 00 04 (Код функции 04, Адрес 0013, Количество 0004)
        // Полное сообщение:  00 00 00 00 00 06 06 04 00 13 00 04
        CheckReadFunction(
            selectedFunction: Function.ReadInputRegisters,
            packageNumber: 0,
            slaveID: 6,
            address: 19,
            numberOfRegisters: 4
            );
    }

    [Fact]
    public void Test_ForceSingleCoil_CreatesCorrectMessage()
    {
        // Ожидаемое Modbus TCP сообщение (Заголовок MBAP + PDU):
        // ID Транзакции:     00 00
        // ID Протокола:      00 00
        // Длина:             00 06 (ID Устройства + длина PDU)
        // ID Устройства:     0D (13)
        // PDU:               05 00 60 FF 00 (Код функции 05, Адрес 0060, Данные для записи FF00)
        // Полное сообщение:  00 00 00 00 00 06 0D 05 00 60 FF 00
        CheckSingleWriteFunction(
            selectedFunction: Function.ForceSingleCoil,
            packageNumber: 0,
            slaveID: 13,
            address: 96,
            writeData: 0xFF00
            );
    }

    [Fact]
    public void Test_PresetSingleRegister_CreatesCorrectMessage()
    {
        // Ожидаемое Modbus TCP сообщение (Заголовок MBAP + PDU):
        // ID Транзакции:     00 00
        // ID Протокола:      00 00
        // Длина:             00 06 (ID Устройства + длина PDU)
        // ID Устройства:     12 (18)
        // PDU:               06 00 3F AE D5 (Код функции 06, Адрес 003F, Данные для записи AED5)
        // Полное сообщение:  00 00 00 00 00 06 12 06 00 3F AE D5
        CheckSingleWriteFunction(
            selectedFunction: Function.PresetSingleRegister,
            packageNumber: 0,
            slaveID: 18,
            address: 63,
            writeData: 0xAED5
            );
    }

    [Fact]
    public void Test_ForceMultipleCoils_CreatesCorrectMessage()
    {
        // Ожидаемое Modbus TCP сообщение (Заголовок MBAP + PDU):
        // ID Транзакции:     00 00
        // ID Протокола:      00 00
        // Длина:             00 0A (ID Устройства + длина PDU: 1+1+2+2+1+1 = 8 байт для PDU, +1 для ID Устройства = 9 байт)
        // ID Устройства:     20 (32)
        // PDU:               0F 00 49 00 07 01 0B (Код функции 0F, Адрес 0049, Количество 0007, Количество байт 01, Данные флагов 0B)
        // Полное сообщение:  00 00 00 00 00 09 20 0F 00 49 00 07 01 0B
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
        // Ожидаемое Modbus TCP сообщение (Заголовок MBAP + PDU):
        // ID Транзакции:     00 00
        // ID Протокола:      00 00
        // Длина:             00 11 (ID Устройства + длина PDU: 1+1+2+2+1+5*2 = 17 байт для PDU, +1 для ID Устройства = 18 байт)
        // ID Устройства:     12 (18)
        // PDU:               10 00 3F 00 05 0A FF FF 45 86 40 00 05 68 FA FD
        // Полное сообщение:  00 00 00 00 00 11 12 10 00 3F 00 05 0A FF FF 45 86 40 00 05 68 FA FD
        CheckMultiplyWriteRegistersFunction(
            packageNumber: 0,
            slaveID: 18,
            address: 63,
            writeData: new UInt16[] { 0xFFFF, 0x4586, 0x4000, 0x0568, 0xFAFD }
            );
    }

    protected override byte[] CreateExpectedReadMessage(byte slaveID, ModbusReadFunction selectedFunction, UInt16 address, UInt16 numberOfRegisters, bool checkSum_IsEnable, UInt16 packageNumber)
    {
        // CheckSum_IsEnable не используется для TCP, всегда false

        byte[] packageNumberArray = BitConverter.GetBytes(packageNumber);
        byte[] addressBytes = ModbusField.Get_Address(address);
        byte[] numberOfRegistersBytes = ModbusField.Get_NumberOfRegisters(numberOfRegisters);

        byte[] bytesArray_Expected = new byte[12];

        bytesArray_Expected[0] = packageNumberArray[1];
        bytesArray_Expected[1] = packageNumberArray[0];
        // Modbus ID
        bytesArray_Expected[2] = 0;
        bytesArray_Expected[3] = 0;
        // Количество байт далее (байт SlaveID + байты PDU)
        bytesArray_Expected[4] = 0;
        bytesArray_Expected[5] = 6;
        bytesArray_Expected[6] = slaveID;
        bytesArray_Expected[7] = selectedFunction.Number;
        bytesArray_Expected[8] = addressBytes[1];
        bytesArray_Expected[9] = addressBytes[0];
        bytesArray_Expected[10] = numberOfRegistersBytes[1];
        bytesArray_Expected[11] = numberOfRegistersBytes[0];

        return bytesArray_Expected;
    }

    protected override byte[] CreateExpectedSingleWriteMessage(byte slaveID, ModbusWriteFunction selectedFunction, UInt16 address, UInt16 writeData, bool checkSum_IsEnable, UInt16 packageNumber)
    {
        // CheckSum_IsEnable не используется для TCP, всегда false

        UInt16[] writeDataArray = new UInt16[] { writeData };

        byte[] packageNumberArray = BitConverter.GetBytes(packageNumber);
        byte[] addressBytes = ModbusField.Get_Address(address);
        byte[] writeDataBytes = ModbusField.Get_WriteData(writeDataArray);

        if (writeDataBytes.Length != 2)
        {
            throw new Exception("При записи одного регистра поле данных должно содержать только 2 байта.");
        }

        byte[] bytesArray_Expected = new byte[12];

        bytesArray_Expected[0] = packageNumberArray[1];
        bytesArray_Expected[1] = packageNumberArray[0];
        // Modbus ID
        bytesArray_Expected[2] = 0;
        bytesArray_Expected[3] = 0;
        // Количество байт далее (байт SlaveID + байты PDU)
        bytesArray_Expected[4] = 0;
        bytesArray_Expected[5] = 6;
        bytesArray_Expected[6] = slaveID;
        bytesArray_Expected[7] = selectedFunction.Number;
        bytesArray_Expected[8] = addressBytes[1];
        bytesArray_Expected[9] = addressBytes[0];
        bytesArray_Expected[10] = writeDataBytes[0];
        bytesArray_Expected[11] = writeDataBytes[1];

        return bytesArray_Expected;
    }

    protected override byte[] CreateExpectedMultiplyWriteCoilsMessage(byte slaveID, ModbusWriteFunction selectedFunction, UInt16 address, int[] bitArray, bool checkSum_IsEnable, UInt16 packageNumber)
    {
        // CheckSum_IsEnable не используется для TCP, всегда false

        (byte[] writeBytes, int numberOfCoils) = ModbusField.Get_WriteDataFromMultipleCoils(bitArray);

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

        return bytesArray_Expected;
    }

    protected override byte[] CreateExpectedMultiplyWriteRegistersMessage(byte slaveID, ModbusWriteFunction selectedFunction, UInt16 address, UInt16[] writeData, bool checkSum_IsEnable, UInt16 packageNumber)
    {
        // CheckSum_IsEnable не используется для TCP, всегда false

        byte[] addressBytes = ModbusField.Get_Address(address);
        byte[] numberOfRegisters = ModbusField.Get_NumberOfRegisters((UInt16)writeData.Length);
        byte[] writeDataBytes = ModbusField.Get_WriteData(writeData);

        byte[] packageNumberArray = BitConverter.GetBytes(packageNumber);

        // PDU - 6 байт + байт SlaveID + байты данных
        byte[] slaveID_PDU_Size_Bytes = BitConverter.GetBytes((UInt16)(7 + writeDataBytes.Length));

        if (writeDataBytes.Length != writeData.Length * 2)
        {
            throw new Exception("Неправильное количество байт в поле данных.");
        }

        byte[] bytesArray_Expected = new byte[13 + writeDataBytes.Length];

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
        bytesArray_Expected[12] = (byte)writeDataBytes.Length;

        Array.Copy(writeDataBytes, 0, bytesArray_Expected, 13, writeDataBytes.Length);

        return bytesArray_Expected;
    }
}
