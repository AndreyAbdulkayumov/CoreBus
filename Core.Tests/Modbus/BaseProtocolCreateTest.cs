using Core.Models.Modbus.DataTypes;
using Core.Models.Modbus.Message;

namespace Core.Tests.Modbus;

public abstract class BaseProtocolCreateTest
{
    protected ModbusMessage Message;

    protected BaseProtocolCreateTest()
    {
        Message = GetModbusMessageInstance();
    }

    // Абстрактный метод, который будет реализован в дочерних классах для создания экземпляра ModbusMessage.
    protected abstract ModbusMessage GetModbusMessageInstance();

    // Абстрактные методы для создания ожидаемых байтовых последовательностей, специфичных для каждого протокола.
    protected abstract byte[] CreateExpectedReadMessage(byte slaveID, ModbusReadFunction selectedFunction, UInt16 address, UInt16 numberOfRegisters, bool checkSum_IsEnable);
    protected abstract byte[] CreateExpectedSingleWriteMessage(byte slaveID, ModbusWriteFunction selectedFunction, UInt16 address, UInt16 writeData, bool checkSum_IsEnable);
    protected abstract byte[] CreateExpectedMultiplyWriteCoilsMessage(byte slaveID, ModbusWriteFunction selectedFunction, UInt16 address, int[] bitArray, bool checkSum_IsEnable);
    protected abstract byte[] CreateExpectedMultiplyWriteRegistersMessage(byte slaveID, ModbusWriteFunction selectedFunction, UInt16 address, UInt16[] writeData, bool checkSum_IsEnable);

    /*
     * 
     * 
     * Обобщенные методы проверки, которые будут использовать абстрактные методы.
     * 
     * 
     */

    protected void CheckReadFunction(ModbusReadFunction selectedFunction, byte slaveID, UInt16 address, UInt16 numberOfRegisters, bool checkSum_IsEnable = false)
    {
        MessageData Data = new ReadTypeMessage(slaveID, address, numberOfRegisters, checkSum_IsEnable);

        byte[] BytesArray_Actual = Message.CreateMessage(selectedFunction, Data);

        byte[] BytesArray_Expected = CreateExpectedReadMessage(slaveID, selectedFunction, address, numberOfRegisters, checkSum_IsEnable);

        Assert.Equal(BytesArray_Expected, BytesArray_Actual);
    }

    protected void CheckSingleWriteFunction(ModbusWriteFunction selectedFunction, byte slaveID, UInt16 address, UInt16 writeData, bool checkSum_IsEnable = false)
    {
        UInt16[] WriteDataArray = new UInt16[] { writeData };

        byte[] bytes = BitConverter.GetBytes(writeData);

        MessageData Data = new WriteTypeMessage(slaveID, address, bytes, 1, checkSum_IsEnable);

        byte[] BytesArray_Actual = Message.CreateMessage(selectedFunction, Data);

        byte[] BytesArray_Expected = CreateExpectedSingleWriteMessage(slaveID, selectedFunction, address, writeData, checkSum_IsEnable);

        Assert.Equal(BytesArray_Expected, BytesArray_Actual);
    }

    protected void CheckMultiplyWriteCoilsFunction(byte slaveID, UInt16 address, int[] bitArray, bool checkSum_IsEnable = false)
    {
        ModbusWriteFunction selectedFunction = Function.ForceMultipleCoils;

        (byte[] writeBytes, int numberOfCoils) = ModbusField.Get_WriteDataFromMultipleCoils(bitArray);

        MessageData data = new WriteTypeMessage(slaveID, address, writeBytes, numberOfCoils, checkSum_IsEnable);

        byte[] bytesArray_Actual = Message.CreateMessage(selectedFunction, data);

        byte[] bytesArray_Expected = CreateExpectedMultiplyWriteCoilsMessage(slaveID, selectedFunction, address, bitArray, checkSum_IsEnable);

        Assert.Equal(bytesArray_Expected, bytesArray_Actual);
    }

    protected void CheckMultiplyWriteRegistersFunction(byte slaveID, UInt16 address, UInt16[] writeData, bool checkSum_IsEnable = false)
    {
        ModbusWriteFunction selectedFunction = Function.PresetMultipleRegisters;

        byte[] bytes = writeData.SelectMany(BitConverter.GetBytes).ToArray();

        MessageData Data = new WriteTypeMessage(slaveID, address, bytes, writeData.Length, checkSum_IsEnable);

        byte[] BytesArray_Actual = Message.CreateMessage(selectedFunction, Data);

        byte[] BytesArray_Expected = CreateExpectedMultiplyWriteRegistersMessage(slaveID, selectedFunction, address, writeData, checkSum_IsEnable);

        Assert.Equal(BytesArray_Expected, BytesArray_Actual);
    }
}
