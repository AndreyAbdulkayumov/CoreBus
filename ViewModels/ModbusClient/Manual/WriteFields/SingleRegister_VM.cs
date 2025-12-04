using ReactiveUI;
using System.Collections.ObjectModel;
using System.Globalization;
using Core.Models.Settings.DataTypes;
using ViewModels.Validation;
using ViewModels.ModbusClient.Manual.DataTypes;
using ViewModels.ModbusClient.Manual.WriteFields.DataTypes;

namespace ViewModels.ModbusClient.Manual.WriteFields;

public class SingleRegister_VM : ModbusDataFormatter, IWriteField_VM
{
    private UInt16 _data = 0;

    private string? _viewData;

    public override string? ViewData
    {
        get => _viewData;
        set
        {
            this.RaiseAndSetIfChanged(ref _viewData, value);
            ValidateInput(nameof(ViewData), value);
        }
    }

    public NumberStyles DataFormat { get; private set; }

    private readonly ObservableCollection<string> _formatItems = new ObservableCollection<string>()
    {
        DataFormatName_dec, DataFormatName_hex, DataFormatName_bin
    };

    public ObservableCollection<string> FormatItems
    {
        get => _formatItems;
    }

    private string? _selectedDataFormat;

    public string? SelectedDataFormat
    {
        get => _selectedDataFormat;
        set => SetDataFormat(value);
    }

    public bool HasValidationErrors => HasErrors;
    public string? ValidationMessage
    {
        get => string.Join("\n", ActualErrors.Select(element => $"[Поле записи данных]\n{GetFullErrorMessage(element.Key)}\n"));
    }

    public SingleRegister_VM()
    {
        SelectedDataFormat = DataFormatName_hex;
    }

    public WriteData GetData()
    {
        return PrepareData();
    }

    private WriteData PrepareData()
    {
        byte[] data = BitConverter.GetBytes(_data);

        return new WriteData(data, 1);
    }

    public void SetDataFromMacros(ModbusMacrosWriteInfo data)
    {
        if (data.WriteBuffer == null || data.WriteBuffer.Length == 0)
        {
            return;
        }

        ViewData = ConvertNumberToString(BitConverter.ToUInt16(data.WriteBuffer), DataFormat);
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

    public override void SetDataFormat(string? format)
    {
        switch (format)
        {
            case DataFormatName_dec:
                DataFormat = NumberStyles.Number;
                break;

            case DataFormatName_hex:
                DataFormat = NumberStyles.HexNumber;
                break;

            case DataFormatName_bin:
                DataFormat = NumberStyles.BinaryNumber;
                break;

            default:
                throw new Exception("Неподдерживаемый формат числа: " + format);
        }

        _selectedDataFormat = format;

        ViewData = ConvertNumberToString(_data, DataFormat);
    }

    protected override ValidateMessage? GetErrorMessage(string fieldName, string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            _data = 0;
            return null;
        }

        if (!StringValue.IsValidNumber(value, DataFormat, out _data))
        {
            switch (DataFormat)
            {
                case NumberStyles.Number:
                    return AllErrorMessages[DecError_UInt16];

                case NumberStyles.HexNumber:
                    return AllErrorMessages[HexError_UInt16];

                case NumberStyles.BinaryNumber:
                    return AllErrorMessages[BinError_UInt16];
            }
        }

        return null;
    }
}
