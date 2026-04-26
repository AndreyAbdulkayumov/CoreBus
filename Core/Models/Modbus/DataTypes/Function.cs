using Services.Interfaces;
using System.ComponentModel;

namespace Core.Models.Modbus.DataTypes;

public abstract class ModbusFunction : INotifyPropertyChanged
{
    private readonly string _displayedNameKey;
    private readonly string _displayedNumberKey;
    public readonly byte Number;

    public string DisplayedName => LocalizationProvider.Get(_displayedNameKey);
    public string DisplayedNumber => LocalizationProvider.Get(_displayedNumberKey);

    public event PropertyChangedEventHandler? PropertyChanged;

    public ModbusFunction(string displayedNameKey, string displayedNumberKey, byte number)
    {
        _displayedNameKey = displayedNameKey;
        _displayedNumberKey = displayedNumberKey;
        Number = number;
    }

    internal void RaiseLocalizationChanged()
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(DisplayedName)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(DisplayedNumber)));
    }
}

public class ModbusReadFunction : ModbusFunction
{
    public ModbusReadFunction(string displayedName, string displayedNumber, byte number) :
        base(displayedName, displayedNumber, number)
    {

    }
}

public class ModbusWriteFunction : ModbusFunction
{
    public ModbusWriteFunction(string displayedName, string displayedNumber, byte number) :
        base(displayedName, displayedNumber, number)
    {

    }
}


public static class Function
{
    static Function()
    {
        try
        {
            LocalizationProvider.Instance.LanguageChanged += (_, _) =>
            {
                foreach (var function in AllFunctions)
                {
                    function.RaiseLocalizationChanged();
                }
            };
        }
        catch (InvalidOperationException)
        {
            // Localization provider can be initialized later during app startup.
        }
    }

    public static readonly ModbusReadFunction ReadCoilStatus =
        new ModbusReadFunction(
            "Core.Modbus.Function.ReadCoilStatus.Name",
            "Core.Modbus.Function.ReadCoilStatus.Number",
            0x01);

    public static readonly ModbusReadFunction ReadDiscreteInputs =
        new ModbusReadFunction(
            "Core.Modbus.Function.ReadDiscreteInputs.Name",
            "Core.Modbus.Function.ReadDiscreteInputs.Number",
            0x02);

    public static readonly ModbusReadFunction ReadHoldingRegisters =
        new ModbusReadFunction(
            "Core.Modbus.Function.ReadHoldingRegisters.Name",
            "Core.Modbus.Function.ReadHoldingRegisters.Number",
            0x03);

    public static readonly ModbusReadFunction ReadInputRegisters =
        new ModbusReadFunction(
            "Core.Modbus.Function.ReadInputRegisters.Name",
            "Core.Modbus.Function.ReadInputRegisters.Number",
            0x04);

    public static readonly ModbusReadFunction[] AllReadFunctions =
        {
            ReadCoilStatus,
            ReadDiscreteInputs,
            ReadHoldingRegisters,
            ReadInputRegisters
        };

    public static readonly ModbusWriteFunction ForceSingleCoil =
        new ModbusWriteFunction(
            "Core.Modbus.Function.ForceSingleCoil.Name",
            "Core.Modbus.Function.ForceSingleCoil.Number",
            0x05);

    public static readonly ModbusWriteFunction PresetSingleRegister =
        new ModbusWriteFunction(
            "Core.Modbus.Function.PresetSingleRegister.Name",
            "Core.Modbus.Function.PresetSingleRegister.Number",
            0x06);

    public static readonly ModbusWriteFunction ForceMultipleCoils =
        new ModbusWriteFunction(
            "Core.Modbus.Function.ForceMultipleCoils.Name",
            "Core.Modbus.Function.ForceMultipleCoils.Number",
            0x0F);

    public static readonly ModbusWriteFunction PresetMultipleRegisters =
        new ModbusWriteFunction(
            "Core.Modbus.Function.PresetMultipleRegisters.Name",
            "Core.Modbus.Function.PresetMultipleRegisters.Number",
            0x10);

    public static readonly ModbusWriteFunction[] AllWriteFunctions =
        {
            ForceSingleCoil,
            PresetSingleRegister,
            ForceMultipleCoils,
            PresetMultipleRegisters
        };

    public static readonly ModbusFunction[] AllFunctions =
        {
            ReadCoilStatus,
            ReadDiscreteInputs,
            ReadHoldingRegisters,
            ReadInputRegisters,
            ForceSingleCoil,
            PresetSingleRegister,
            ForceMultipleCoils,
            PresetMultipleRegisters
        };
}
