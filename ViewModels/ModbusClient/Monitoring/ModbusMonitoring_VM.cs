using Core.Clients.DataTypes;
using Core.Models;
using Core.Models.Modbus.DataTypes;
using MessageBox.Core;
using ReactiveUI;
using Services.Interfaces;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Reactive;

namespace ViewModels.ModbusClient.Monitoring;

public class ModbusMonitoring_VM : ReactiveObject
{
    private bool ui_IsEnable = false;

    public bool UI_IsEnable
    {
        get => ui_IsEnable;
        set => this.RaiseAndSetIfChanged(ref ui_IsEnable, value);
    }

    private ObservableCollection<string> _readFunctions = new ObservableCollection<string>();

    public ObservableCollection<string> ReadFunctions
    {
        get => _readFunctions;
        set => this.RaiseAndSetIfChanged(ref _readFunctions, value);
    }

    private string? _selectedReadFunction;

    public string? SelectedReadFunction
    {
        get => _selectedReadFunction;
        set => this.RaiseAndSetIfChanged(ref _selectedReadFunction, value);
    }

    private bool _isStart = false;

    public bool IsStart
    {
        get => _isStart;
        set => this.RaiseAndSetIfChanged(ref _isStart, value);
    }

    private int _period_ms = 600;

    public int Period_ms
    {
        get => _period_ms;
        set => this.RaiseAndSetIfChanged(ref _period_ms, value);
    }

    private const string Button_Content_Start = "Начать опрос";
    private const string Button_Content_Stop = "Остановить опрос";

    private string _button_Content = Button_Content_Start;

    public string Button_Content
    {
        get => _button_Content;
        set => this.RaiseAndSetIfChanged(ref _button_Content, value);
    }

    private bool _hasSelectedItems;

    public bool HasSelectedItems
    {
        get => _hasSelectedItems;
        set => this.RaiseAndSetIfChanged(ref _hasSelectedItems, value);
    }

    private bool _shouldPollSelectedRegisters;

    public bool ShouldPollSelectedRegisters
    {
        get => _shouldPollSelectedRegisters;
        set => this.RaiseAndSetIfChanged(ref _shouldPollSelectedRegisters, value);
    }

    private bool _allRowSelected;

    public bool AllRowSelected
    {
        get => _allRowSelected;
        set => this.RaiseAndSetIfChanged(ref _allRowSelected, value);
    }

    private ObservableCollection<MonitoringItem_VM> _monitoringItems = new ObservableCollection<MonitoringItem_VM>();

    public ObservableCollection<MonitoringItem_VM> MonitoringItems
    {
        get => _monitoringItems;
        set => this.RaiseAndSetIfChanged(ref _monitoringItems, value);
    }

    public ReactiveCommand<Unit, Unit> Command_Start_Stop_Polling { get; }
    public ReactiveCommand<Unit, Unit> Command_RemoveSelectedItems { get; }
    public ReactiveCommand<Unit, Unit> Command_SelectAllRows { get; }
    public ReactiveCommand<Unit, Unit> Command_AddRegister { get; }


    private readonly IMessageBoxMainWindow _messageBox;
    private readonly ConnectedHost _connectedHostModel;

    public ModbusMonitoring_VM(IMessageBoxMainWindow messageBox, ConnectedHost connectedHostModel)
    {
        _messageBox = messageBox ?? throw new ArgumentNullException(nameof(messageBox));
        _connectedHostModel = connectedHostModel ?? throw new ArgumentNullException(nameof(connectedHostModel));

        _connectedHostModel.DeviceIsConnect += Model_DeviceIsConnect;
        _connectedHostModel.DeviceIsDisconnected += Model_DeviceIsDisconnected;

        /****************************************************/
        //
        // Первоначальная настройка UI
        //
        /****************************************************/

        foreach (ModbusReadFunction element in Function.AllReadFunctions)
        {
            ReadFunctions.Add(element.DisplayedName);
        }

        SelectedReadFunction = Function.ReadInputRegisters.DisplayedName;

        /****************************************************/
        //
        // Настройка свойств и команд модели отображения
        //
        /****************************************************/

        Command_Start_Stop_Polling = ReactiveCommand.Create(() =>
        {
            if (IsStart)
            {
                StopPolling();
                return;
            }

            StartPolling();
        });
        Command_Start_Stop_Polling.ThrownExceptions.Subscribe(error => messageBox.Show(error.Message, MessageType.Error, error));

        Command_RemoveSelectedItems = ReactiveCommand.Create(() =>
        {
            for (int i = MonitoringItems.Count - 1; i >= 0; i--)
            {
                if (MonitoringItems[i].IsSelected)
                {
                    MonitoringItems[i].PropertyChanged -= MonitoringItemOnPropertyChanged;
                    MonitoringItems.RemoveAt(i);
                }
            }

            if (MonitoringItems.Count == 0)
                AllRowSelected = false;
        });
        Command_RemoveSelectedItems.ThrownExceptions.Subscribe(error => messageBox.Show($"Ошибка удаления выбранных регистров.\n\n{error.Message}", MessageType.Error, error));

        Command_SelectAllRows = ReactiveCommand.Create(() =>
        {
            foreach (var item in MonitoringItems)
            {
                item.PropertyChanged -= MonitoringItemOnPropertyChanged;
                item.IsSelected = AllRowSelected;
                HasSelectedItems = AllRowSelected;
                item.PropertyChanged += MonitoringItemOnPropertyChanged;
            }
        });
        Command_SelectAllRows.ThrownExceptions.Subscribe(error => messageBox.Show($"Ошибка выбора всех регистров.\n\n{error.Message}", MessageType.Error, error));

        Command_AddRegister = ReactiveCommand.Create(() =>
        {
            int initAddress = MonitoringItems.Any() ? int.Parse(MonitoringItems.Last().Address) + 1 : 0;

            var newItem = new MonitoringItem_VM(initAddress, _messageBox);
            newItem.PropertyChanged += MonitoringItemOnPropertyChanged;

            MonitoringItems.Add(newItem);
        });
        Command_AddRegister.ThrownExceptions.Subscribe(error => _messageBox.Show($"Ошибка добавления регистра.\n\n{error.Message}", MessageType.Error, error));
    }

    private void MonitoringItemOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MonitoringItem_VM.IsSelected))
        {
            AllRowSelected = MonitoringItems.Count > 0 &&
                             MonitoringItems.All(x => x.IsSelected);
        }

        foreach (var item in MonitoringItems)
        {
            if (item.IsSelected)
            {
                HasSelectedItems = true;
                return;
            }
        }

        HasSelectedItems = false;
    }
    private void Model_DeviceIsConnect(object? sender, IConnection? e)
    {
        UI_IsEnable = true;
    }

    private void Model_DeviceIsDisconnected(object? sender, IConnection? e)
    {
        UI_IsEnable = false;

        StopPolling();
    }

    private void StartPolling()
    {
        Button_Content = Button_Content_Stop;
        IsStart = true;
    }

    public void StopPolling()
    {
        Button_Content = Button_Content_Start;
        IsStart = false;
    }    
}
