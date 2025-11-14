using ReactiveUI;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Reactive;
using Core.Models.Settings;
using Core.Models.Settings.DataTypes;
using Core.Models.Settings.FileTypes;
using ViewModels.Helpers.FloatNumber;
using ViewModels.ModbusClient.DataTypes;
using ViewModels.ModbusClient.WriteFields.DataItems;
using ViewModels.ModbusClient.WriteFields.DataTypes;

namespace ViewModels.ModbusClient.WriteFields;

public class MultipleRegisters_VM : ReactiveObject, IWriteField_VM
{
    private bool _floatFormatChangeIsEnabled;

    public bool FloatFormatChangeIsEnabled
    {
        get => _floatFormatChangeIsEnabled;
        set => this.RaiseAndSetIfChanged(ref _floatFormatChangeIsEnabled, value);
    }

    private ObservableCollection<string> _floatFormats = new ObservableCollection<string>()
    {
        DeviceData.FloatWriteFormat_AB_CD,
        DeviceData.FloatWriteFormat_BA_DC,
        DeviceData.FloatWriteFormat_CD_AB,
        DeviceData.FloatWriteFormat_DC_BA,
    };

    public ObservableCollection<string> FloatFormats
    {
        get => _floatFormats;
    }

    private string? _selectedFloatFormat;

    public string? SelectedFloatFormat
    {
        get => _selectedFloatFormat;
        set => this.RaiseAndSetIfChanged(ref _selectedFloatFormat, value);
    }

    private ObservableCollection<MultipleRegisters_Item> _writeDataCollection = new ObservableCollection<MultipleRegisters_Item>();

    public ObservableCollection<MultipleRegisters_Item> WriteDataCollection
    {
        get => _writeDataCollection;
        set => this.RaiseAndSetIfChanged(ref _writeDataCollection, value);
    }

    public bool HasValidationErrors => WriteDataCollection.Any(e => e.HasErrors);
    public string? ValidationMessage => string.Join("\n\n",
        WriteDataCollection
            .Select((f, index) => new { Field = f, Index = index + 1 })
            .Where(item => item.Field.HasErrors)
            .SelectMany(item =>
                item.Field.ActualErrors.Select(element => $"[Поле записи данных №{item.Index}]\n{item.Field.GetFullErrorMessage(element.Key)}")
            )
    );

    public ReactiveCommand<Unit, Unit> Command_AddRegister { get; }

    private List<int> _floatStartByteIndices = new List<int>();

    private readonly Model_Settings _settingsModel;

    public MultipleRegisters_VM(bool floatFormatChangeIsEnabled, Model_Settings settingsModel)
    {
        _settingsModel = settingsModel ?? throw new ArgumentNullException(nameof(settingsModel));

        FloatFormatChangeIsEnabled = floatFormatChangeIsEnabled;

        SelectedFloatFormat = DeviceData.FloatWriteFormat_BA_DC;

        Command_AddRegister = ReactiveCommand.Create(() =>
        {
            WriteDataCollection.Add(new MultipleRegisters_Item(
                startAddressAddition: GetAddressAddition(),
                initWordValue: null,
                initFloatValue: null,
                removeItemHandler: RemoveWriteDataItem
                ));

            WriteDataCollection.Last().RequestToUpdateAddresses += Item_RequestToUpdateAddresses;
        });
    }

    private int GetAddressAddition()
    {
        if (WriteDataCollection.Count == 0)
        {
            return 0;
        }

        else
        {
            int formatAddition = WriteDataCollection.Last().SelectedDataFormat == ModbusDataFormatter.DataFormatName_float ? 2 : 1;
            return WriteDataCollection.Last().StartAddressAddition + formatAddition;
        }
    }

    public WriteData GetData()
    {
        return PrepareData(_settingsModel.Settings?.FloatNumberFormat);
    }

    private WriteData PrepareData(string? floatFormatName)
    {
        _floatStartByteIndices.Clear();

        if (WriteDataCollection.Count == 0)
        {
            return new WriteData(Array.Empty<byte>(), 0);
        }

        FloatNumberFormat floatFormat = FloatHelper.GetFloatNumberFormatOrDefault(floatFormatName);

        int registerCounter = 0;

        byte[] data = WriteDataCollection.SelectMany(x =>
        {
            if (x.DataFormat == NumberStyles.Float)
            {
                // т.к. один регистр содержит два байта.
                _floatStartByteIndices.Add(registerCounter * 2);

                registerCounter += 2;

                return FloatHelper.GetBytesFromFloatNumber(x.FloatData, floatFormat);
            }

            registerCounter++;

            return BitConverter.GetBytes(x.Data);
        })
        .ToArray();

        return new WriteData(data, registerCounter);
    }

    public void SetDataFromMacros(ModbusMacrosWriteInfo data)
    {
        WriteDataCollection.Clear();

        SelectedFloatFormat = data.FloatNumberFormat;

        if (data.WriteBuffer == null || data.FloatStartByteIndices == null || data.WriteBuffer.Length == 0)
        {
            return;
        }

        FloatNumberFormat floatFormat = FloatHelper.GetFloatNumberFormatOrDefault(data.FloatNumberFormat);

        int byteCounter = 0;

        float floatValue = 0;
        UInt16 wordValue = 0;

        do
        {
            if (data.FloatStartByteIndices.Contains(byteCounter))
            {
                byte[] temp = data.WriteBuffer.Take(new Range(byteCounter, byteCounter + 4)).ToArray();

                Array.Reverse(temp); // т.к. в протоколе Modbus используется передача данных старшим байтом вперед.

                floatValue = FloatHelper.GetFloatNumberFromBytes(temp, floatFormat);

                WriteDataCollection.Add(new MultipleRegisters_Item(
                    startAddressAddition: GetAddressAddition(),
                    initWordValue: null,
                    initFloatValue: floatValue,
                    removeItemHandler: RemoveWriteDataItem
                    ));

                byteCounter += 4;
                continue;
            }

            wordValue = BitConverter.ToUInt16(data.WriteBuffer.Take(new Range(byteCounter, byteCounter + 2)).ToArray());

            WriteDataCollection.Add(new MultipleRegisters_Item(
                startAddressAddition: GetAddressAddition(),
                initWordValue: wordValue,
                initFloatValue: null,
                removeItemHandler: RemoveWriteDataItem
                ));

            byteCounter += 2;

        } while (byteCounter < data.WriteBuffer.Length);
    }

    public ModbusMacrosWriteInfo GetMacrosData()
    {
        WriteData data = PrepareData(SelectedFloatFormat);

        return new ModbusMacrosWriteInfo()
        {
            WriteBuffer = data.Data,
            NumberOfWriteRegisters = data.NumberOfRegisters,
            FloatNumberFormat = SelectedFloatFormat,
            FloatStartByteIndices = _floatStartByteIndices.ToArray(),
        };
    }

    private void Item_RequestToUpdateAddresses(object? sender, RequestToUpdateAddressesArgs e)
    {
        int itemIndex = WriteDataCollection.ToList().FindIndex(item => item.Id == e.ItemId);

        if (itemIndex < WriteDataCollection.Count - 1)
        {
            UpdateAddresses();
        }
    }

    private void RemoveWriteDataItem(Guid selectedId)
    {
        int itemIndex = WriteDataCollection.ToList().FindIndex(item => item.Id == selectedId);

        WriteDataCollection.RemoveAt(itemIndex);

        UpdateAddresses();
    }

    private void UpdateAddresses()
    {
        int AddressCounter = 0;

        foreach (var item in WriteDataCollection)
        {
            item.StartAddressAddition = AddressCounter;

            if (item.SelectedDataFormat == ModbusDataFormatter.DataFormatName_float)
            {
                AddressCounter += 2;
                continue;
            }

            AddressCounter += 1;
        }
    }
}
