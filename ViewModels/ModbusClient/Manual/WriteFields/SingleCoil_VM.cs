using ReactiveUI;
using Core.Models.Settings.DataTypes;
using ViewModels.ModbusClient.Manual.DataTypes;
using ViewModels.ModbusClient.Manual.WriteFields.DataTypes;

namespace ViewModels.ModbusClient.Manual.WriteFields;

public class SingleCoil_VM : ReactiveObject, IWriteField_VM
{
    private bool _logic_Zero;

    public bool Logic_Zero
    {
        get => _logic_Zero;
        set => this.RaiseAndSetIfChanged(ref _logic_Zero, value);
    }

    private bool _logic_One;

    public bool Logic_One
    {
        get => _logic_One;
        set => this.RaiseAndSetIfChanged(ref _logic_One, value);
    }

    // У этого элемента нет полей ввода, поэтому он не может иметь ошибок валидации
    public bool HasValidationErrors => false;
    public string? ValidationMessage => null;

    // 0xFF00
    private const byte LogicOne_Value_High = 0xFF;
    private const byte LogicOne_Value_Low = 0x00;

    private const byte LogicZero_Value_High = 0;
    private const byte LogicZero_Value_Low = 0;

    private readonly byte[] LogicOneData = { LogicOne_Value_Low, LogicOne_Value_High };
    private readonly byte[] LogicZeroData = { LogicZero_Value_Low, LogicZero_Value_High };

    public SingleCoil_VM()
    {
        Logic_One = true;
    }

    public WriteData GetData()
    {
        return PrepareData();
    }

    private WriteData PrepareData()
    {
        byte[] data = Logic_One ?
           LogicOneData :
           LogicZeroData;

        return new WriteData(data, 1);
    }

    public void SetDataFromMacros(ModbusMacrosWriteInfo data)
    {
        if (data.WriteBuffer == null || data.WriteBuffer.Length == 0)
        {
            return;
        }

        if (data.WriteBuffer.SequenceEqual(LogicOneData))
        {
            Logic_Zero = false;
            Logic_One = true;
            return;
        }

        Logic_Zero = true;
        Logic_One = false;
    }

    public ModbusMacrosWriteInfo GetMacrosData()
    {
        WriteData data = PrepareData();

        return new ModbusMacrosWriteInfo()
        {
            WriteBuffer = data.Data,
            NumberOfWriteRegisters = data.NumberOfRegisters,
        };
    }
}
