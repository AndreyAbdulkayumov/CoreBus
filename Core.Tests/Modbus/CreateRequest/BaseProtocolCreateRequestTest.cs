using Core.Models.Modbus.DataTypes;
using Core.Models.Modbus.Message;
using Core.Tests.Infrastructure;
using Core.Tests.Modbus.Helpers;

namespace Core.Tests.Modbus.CreateRequest;

public abstract class BaseProtocolCreateRequestTest
{
    private readonly ModbusMessage _message;
    private readonly ILocalizationService _localization;

    protected BaseProtocolCreateRequestTest()
    {
        _message = GetModbusMessageInstance();
        _localization = new TestLocalizationService();
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
        var data = new ReadTypeMessage(slaveID, address, numberOfRegisters, checkSum_IsEnable);

        var bytesArrayActual = _message.CreateRequest(selectedFunction, data, _localization);

        var bytesArrayExpected = CreateExpectedReadMessage(slaveID, selectedFunction, address, numberOfRegisters, checkSum_IsEnable);

        Assert.Equal(bytesArrayExpected, bytesArrayActual);
    }

    protected void CheckSingleWriteFunction(ModbusWriteFunction selectedFunction, byte slaveID, UInt16 address, UInt16 writeData, bool checkSum_IsEnable = false)
    {
        var bytes = BitConverter.GetBytes(writeData);

        var data = new WriteTypeMessage(slaveID, address, bytes, 1, checkSum_IsEnable);

        var bytesArrayActual = _message.CreateRequest(selectedFunction, data, _localization);

        var bytesArrayExpected = CreateExpectedSingleWriteMessage(slaveID, selectedFunction, address, writeData, checkSum_IsEnable);

        Assert.Equal(bytesArrayExpected, bytesArrayActual);
    }

    protected void CheckMultiplyWriteCoilsFunction(byte slaveID, UInt16 address, int[] bitArray, bool checkSum_IsEnable = false)
    {
        var selectedFunction = Function.ForceMultipleCoils;

        (byte[] writeBytes, int numberOfCoils) = ModbusField.Get_WriteDataFromMultipleCoils(bitArray);

        var data = new WriteTypeMessage(slaveID, address, writeBytes, numberOfCoils, checkSum_IsEnable);

        var bytesArrayActual = _message.CreateRequest(selectedFunction, data, _localization);

        var bytesArrayExpected = CreateExpectedMultiplyWriteCoilsMessage(slaveID, selectedFunction, address, bitArray, checkSum_IsEnable);

        Assert.Equal(bytesArrayExpected, bytesArrayActual);
    }

    protected void CheckMultiplyWriteRegistersFunction(byte slaveID, UInt16 address, UInt16[] writeData, bool checkSum_IsEnable = false)
    {
        var selectedFunction = Function.PresetMultipleRegisters;

        var bytes = writeData.SelectMany(BitConverter.GetBytes).ToArray();

        var data = new WriteTypeMessage(slaveID, address, bytes, writeData.Length, checkSum_IsEnable);

        var bytesArrayActual = _message.CreateRequest(selectedFunction, data, _localization);

        var bytesArrayExpected = CreateExpectedMultiplyWriteRegistersMessage(slaveID, selectedFunction, address, writeData, checkSum_IsEnable);

        Assert.Equal(bytesArrayExpected, bytesArrayActual);
    }
}
