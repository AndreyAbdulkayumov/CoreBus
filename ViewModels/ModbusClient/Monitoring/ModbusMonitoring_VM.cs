using MessageBox.Core;
using ReactiveUI;
using Services.Interfaces;
using System.Collections.ObjectModel;
using System.Reactive;

namespace ViewModels.ModbusClient.Monitoring;

public class ModbusMonitoring_VM : ReactiveObject
{
    private ObservableCollection<MonitoringItem_VM> _monitoringItems = new ObservableCollection<MonitoringItem_VM>();

    public ObservableCollection<MonitoringItem_VM> MonitoringItems
    {
        get => _monitoringItems;
        set => this.RaiseAndSetIfChanged(ref _monitoringItems, value);
    }


    public ReactiveCommand<Unit, Unit> Command_AddRegister { get; }

    private readonly IMessageBoxMainWindow _messageBox;

    public ModbusMonitoring_VM(IMessageBoxMainWindow messageBox)
    {
        _messageBox = messageBox ?? throw new ArgumentNullException(nameof(messageBox));

        Command_AddRegister = ReactiveCommand.Create(() =>
        {
            MonitoringItems.Add(new MonitoringItem_VM(_messageBox));
        });
        Command_AddRegister.ThrownExceptions.Subscribe(error => _messageBox.Show($"Ошибка добавления регистра.\n\n{error.Message}", MessageType.Error, error));
    }
}
