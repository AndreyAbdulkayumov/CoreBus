using ReactiveUI;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Reactive;
using ViewModels.ModbusClient.Manual.WriteFields;
using ViewModels.ModbusClient.Manual.WriteFields.DataTypes;
using ViewModels.Validation;

namespace ViewModels.ModbusClient.Manual.WriteFields.DataItems;

public class MultipleRegisters_Item : ModbusDataFormatter
{
    public event EventHandler<RequestToUpdateAddressesArgs>? RequestToUpdateAddresses;

    private int _startAddressAddition;

    public int StartAddressAddition
    {
        get => _startAddressAddition;
        set
        {
            _startAddressAddition = value;
            StartAddressAdditionView = $"+{value}";
        }
    }

    private string? _startAddressAdditionView;

    public string? StartAddressAdditionView
    {
        get => _startAddressAdditionView;
        set => this.RaiseAndSetIfChanged(ref _startAddressAdditionView, value);
    }

    public float FloatData = 0f;

    private UInt16 _data = 0;

    public UInt16 Data => _data;

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
        DataFormatName_dec, DataFormatName_hex, DataFormatName_bin, DataFormatName_float
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

    public readonly Guid Id;

    public ReactiveCommand<Unit, Unit>? Command_RemoveItem { get; set; }


    public MultipleRegisters_Item(
        int startAddressAddition,
        UInt16? initWordValue,
        float? initFloatValue,
        Action<Guid> removeItemHandler)
    {
        Id = Guid.NewGuid();

        StartAddressAddition = startAddressAddition;

        if (initWordValue != null)
        {
            _data = (UInt16)initWordValue;
            SelectedDataFormat = DataFormatName_hex;
        }

        else if (initFloatValue != null)
        {
            FloatData = (float)initFloatValue;
            SelectedDataFormat = DataFormatName_float;
        }

        else
        {
            SelectedDataFormat = DataFormatName_hex;
        }

        Command_RemoveItem = ReactiveCommand.Create(() =>
        {
            removeItemHandler?.Invoke(Id);
        });
    }

    public override void SetDataFormat(string? newFormat)
    {
        switch (newFormat)
        {
            case DataFormatName_dec:
                DataFormat = NumberStyles.Number;
                ViewData = ConvertNumberToString(_data, DataFormat);
                break;

            case DataFormatName_hex:
                DataFormat = NumberStyles.HexNumber;
                ViewData = ConvertNumberToString(_data, DataFormat);
                break;

            case DataFormatName_bin:
                DataFormat = NumberStyles.BinaryNumber;
                ViewData = ConvertNumberToString(_data, DataFormat);
                break;

            case DataFormatName_float:
                DataFormat = NumberStyles.Float;
                ViewData = FloatData.ToString("F", CultureInfo.InvariantCulture);
                break;

            default:
                throw new Exception("Неподдерживаемый формат числа: " + newFormat);
        }

        bool needUpdate = newFormat == DataFormatName_float || _selectedDataFormat == DataFormatName_float;

        _selectedDataFormat = newFormat;

        if (needUpdate)
        {
            RequestToUpdateAddresses?.Invoke(this, new RequestToUpdateAddressesArgs(Id, newFormat));
        }
    }

    protected override ValidateMessage? GetErrorMessage(string fieldName, string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            if (DataFormat == NumberStyles.Float)
            {
                FloatData = 0;
            }

            else
            {
                _data = 0;
            }

            return null;
        }

        if (DataFormat == NumberStyles.Float && !StringValue.IsValidNumber(value, DataFormat, out FloatData))
        {
            return AllErrorMessages[DecError_float];
        }

        if (DataFormat != NumberStyles.Float && !StringValue.IsValidNumber(value, DataFormat, out _data))
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
