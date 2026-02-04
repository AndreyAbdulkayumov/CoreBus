using Core.Models.Modbus.DataTypes;
using Core.Models.Settings;
using Core.Models.Settings.FileTypes;
using MessageBox.Core;
using ReactiveUI;
using Services.Interfaces;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Globalization;
using System.Reactive;
using ViewModels.ModbusClient.Manual;

namespace ViewModels.ModbusClient.Monitoring;

public class MonitoringDataGrid_VM : ReactiveObject
{
    public bool IsEmpty => !Items.Any();

    private bool _hasSelectedItems;

    public bool HasSelectedItems
    {
        get => _hasSelectedItems;
        set => this.RaiseAndSetIfChanged(ref _hasSelectedItems, value);
    }

    private bool _allRowSelected;

    public bool AllRowSelected
    {
        get => _allRowSelected;
        set => this.RaiseAndSetIfChanged(ref _allRowSelected, value);
    }

    private ObservableCollection<MonitoringItem_VM> _items = new ObservableCollection<MonitoringItem_VM>();

    public ObservableCollection<MonitoringItem_VM> Items
    {
        get => _items;
        set => this.RaiseAndSetIfChanged(ref _items, value);
    }

    private string? _numberFormat;

    public string? NumberFormat
    {
        get => _numberFormat;
        set => this.RaiseAndSetIfChanged(ref _numberFormat, value);
    }

    private bool _blockAddRegisterButton = false;

    public bool BlockAddRegisterButton
    {
        get => _blockAddRegisterButton;
        set => this.RaiseAndSetIfChanged(ref _blockAddRegisterButton, value);
    }

    public ReactiveCommand<Unit, Unit> Command_SelectAllRows { get; }
    public ReactiveCommand<Unit, Unit> Command_AddRegister { get; }


    private NumberStyles _numberViewStyle;

    private readonly Model_Settings _settingsModel;
    private readonly IOpenChildWindowService _openChildWindowService;
    private readonly IMessageBoxMainWindow _messageBox;

    private bool _ignorePropertyChanged;

    public MonitoringDataGrid_VM(Model_Settings settingsModel, IOpenChildWindowService openChildWindowService, IMessageBoxMainWindow messageBox)
    {
        _settingsModel = settingsModel ?? throw new ArgumentNullException(nameof(settingsModel));
        _openChildWindowService = openChildWindowService ?? throw new ArgumentNullException(nameof(openChildWindowService));
        _messageBox = messageBox ?? throw new ArgumentNullException(nameof(messageBox));

        Command_SelectAllRows = ReactiveCommand.Create(() =>
        {
            _ignorePropertyChanged = true;

            foreach (var item in Items)
            {
                item.IsSelected = AllRowSelected;
            }

            _ignorePropertyChanged = false;

            HasSelectedItems = AllRowSelected;
        });
        Command_SelectAllRows.ThrownExceptions.Subscribe(error => messageBox.Show($"Ошибка выбора всех регистров.\n\n{error.Message}", MessageType.Error, error));

        Command_AddRegister = ReactiveCommand.Create(() =>
        {
            var newItem = CreateNewItem(null);

            newItem.TypeChanged += MonitoringItem_TypeChanged;
            newItem.PropertyChanged += MonitoringItem_PropertyChanged;

            Items.Add(newItem);

            AllRowSelected = false;
        });
        Command_AddRegister.ThrownExceptions.Subscribe(error => _messageBox.Show($"Ошибка добавления регистра.\n\n{error.Message}", MessageType.Error, error));

        // Действия после запуска приложения

        var presetItems = _settingsModel.ModbusMonitoringItems.Items;

        if (presetItems != null)
        {
            Items = new ObservableCollection<MonitoringItem_VM>(presetItems.Select(CreateNewItem));
            SetAvailableValueTypes();
        }

        // Подписка на это событие должна быть именно после возможного изменения Items
        Items.CollectionChanged += (object? sender, NotifyCollectionChangedEventArgs e) => SetAvailableValueTypes();        
    }

    private MonitoringItem_VM CreateNewItem(ModbusMonitoringItemData? initData)
    {
        int initAddress = initData?.Address ?? (Items.Count > 0 ? Items.Last().SelectedAddress + 1 : 0);

        var newItem = new MonitoringItem_VM(initAddress, _numberViewStyle, _settingsModel, _openChildWindowService, _messageBox);

        if (initData != null)
        {
            newItem.SetExistingValues(initData);
        }

        newItem.TypeChanged += MonitoringItem_TypeChanged;
        newItem.PropertyChanged += MonitoringItem_PropertyChanged;

        return newItem;
    }    

    private void SetAvailableValueTypes()
    {
        if (Items.Count == 0)
            return;

        var lastIndex = Items.Count - 1;

        for (int i = 0; i < lastIndex; i++)
        {
            Items[i].SetAsLast(false);
        }

        Items[lastIndex].SetAsLast(true);
    }

    private void MonitoringItem_TypeChanged(object? sender, EventArgs e)
    {
        if (sender == null || sender is not MonitoringItem_VM)
            return;

        int registerSkipCounter = 0;

        foreach (var item in Items)
        {
            if (registerSkipCounter > 0)
            {
                item.VisibleOnlyRawValue = true;
                registerSkipCounter--;
                continue;
            }

            item.VisibleOnlyRawValue = false;

            switch (item.SelectedValueType)
            {
                case MonitoringItem_VM.TypeName_UInt16:
                case MonitoringItem_VM.TypeName_Int16:
                    registerSkipCounter = 0;
                    break;

                case MonitoringItem_VM.TypeName_UInt32:
                case MonitoringItem_VM.TypeName_Int32:
                case MonitoringItem_VM.TypeName_Float:
                    registerSkipCounter = 1;
                    break;

                default:
                    registerSkipCounter = 0;
                    break;
            }
        }
    }

    private void MonitoringItem_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (_ignorePropertyChanged)
            return;

        if (sender is not MonitoringItem_VM)
            return;

        if (e.PropertyName == nameof(MonitoringItem_VM.IsSelected))
        {
            AllRowSelected = Items.Count > 0 &&
                             Items.All(x => x.IsSelected);
        }

        foreach (var item in Items)
        {
            if (item.IsSelected)
            {
                HasSelectedItems = true;
                return;
            }
        }

        HasSelectedItems = false;
    }

    public void RemoveSelectedItems()
    {
        for (int i = Items.Count - 1; i >= 0; i--)
        {
            if (Items[i].IsSelected)
            {
                Items[i].TypeChanged -= MonitoringItem_TypeChanged;
                Items[i].PropertyChanged -= MonitoringItem_PropertyChanged;
                Items.RemoveAt(i);
            }
        }

        HasSelectedItems = false;

        if (Items.Count == 0)
            AllRowSelected = false;
    }

    public void ClearData()
    {
        foreach (var item in Items)
        {
            item.Clear();
        }
    }

    public void ChangeNumberFormat(NumberStyles newStyle)
    {
        _numberViewStyle = newStyle;

        NumberFormat = newStyle == NumberStyles.HexNumber ?
            ModbusManualMode_VM.ViewContent_NumberStyle_hex :
            ModbusManualMode_VM.ViewContent_NumberStyle_dec;

        foreach (var item in Items)
        {
            item.SetNumberFormat(newStyle);
        }
    }

    public void BlockUI(bool value)
    {
        BlockAddRegisterButton = value;

        foreach (var item in Items)
        {
            item.UI_IsEnable = !value;
        }
    }

    public void DisplayData(byte[] data, ModbusReadFunction readFunction, UInt16 startingAddress, int numberOfRegisters, uint chartIncrementX)
    {
        var readRegisters =
            ConvertToResultList(data, numberOfRegisters, readFunction)
            .Select((value, index) => (address: startingAddress + index, value))
            .ToDictionary(e => e.address, e => e.value);

        // Тут важно сопоставить порядок регистров с Items, иначе может сломаться преобразование в многобайтовые числа.
        var registers = Items
            .Select(e => (int)e.SelectedAddress)
            .Where(readRegisters.ContainsKey)
            .Select(key => new KeyValuePair<int, ushort>(key, readRegisters[key]));

        foreach (var item in Items)
        {
            if (readRegisters.TryGetValue(item.SelectedAddress, out UInt16 registerValue))
            {
                item.SetReadedValue(registerValue, registers, chartIncrementX);
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
