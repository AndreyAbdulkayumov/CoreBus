using Core.Models.Modbus.DataTypes;
using Core.Models.Modbus.Message;
using ViewModels.Validation;

namespace ViewModels.ModbusClient.Monitoring;

public partial class ModbusMonitoring_VM : ValidatedDateInput, IValidationFieldInfo
{
    private async Task MonitoringRequestAction()
    {
        if (_connectedHostModel.HostIsConnect == false)
        {
            throw new Exception("Клиент отключен.");
        }

        if (ModbusClient_VM.ModbusMessageType == null)
        {
            throw new Exception("Не задан тип протокола Modbus.");
        }

        var allAddresses = MonitoringItems.Select(e => e.SelectedAddress);

        ushort startingAddress = allAddresses.Min();
        int numberOfRegisters = allAddresses.Max() - allAddresses.Min() + 1;

        ModbusReadFunction readFunction = Function.ReadInputRegisters;

        MessageData data = new ReadTypeMessage(
            _selectedSlaveID,
            startingAddress,
            numberOfRegisters,
            ModbusClient_VM.ModbusMessageType is ModbusTCP_Message ? false : true);

        ModbusOperationResult result = await _modbusModel.ReadRegister(
                        readFunction,
                        data,
                        ModbusClient_VM.ModbusMessageType);

        if (result.ReadedData == null)
            return;

        var registers = 
            ConvertToResultList(result.ReadedData, numberOfRegisters, readFunction)
            .Select((value, index) => (address: startingAddress + index, value))
            .ToDictionary(e => e.address, e => e.value);

        foreach (var item in MonitoringItems)
        {
            if (registers.TryGetValue(item.SelectedAddress, out UInt16 registerValue))
            {
                item.SetReadedValue(registerValue);
            }
        }
    }

    private static List<UInt16> ConvertToResultList(byte[] modbusData, int numberOfRegisters, ModbusReadFunction function)
    {
        if (function.Number == Function.ReadCoilStatus.Number ||
            function.Number == Function.ReadDiscreteInputs.Number)
        {
            return GetResultFromBytes(modbusData, numberOfRegisters);
        }

        return GetResultFromWords(modbusData);
    }

    private static List<UInt16> GetResultFromBytes(byte[] modbusData, int numberOfRegisters)
    {
        var result = new List<UInt16>();

        int registerCounter = 0;

        foreach (byte element in modbusData)
        {
            for (int i = 0; i < 8; i++)
            {
                if (registerCounter == numberOfRegisters) break;

                result.Add((UInt16)((element & (1 << (i))) != 0 ? 1 : 0));
                registerCounter++;
            }
        }

        return result;
    }

    private static List<UInt16> GetResultFromWords(byte[] modbusData)
    {
        var result = new List<UInt16>();

        for (int i = 0; i < modbusData.Length - 1; i += 2)
        {
            result.Add((UInt16)((modbusData[i + 1] << 8) | modbusData[i]));
        }

        // Обработка последнего байта, если длина массива нечетная
        if (modbusData.Length % 2 != 0)
        {
            result.Add(modbusData.Last());
        }

        return result;
    }
}
