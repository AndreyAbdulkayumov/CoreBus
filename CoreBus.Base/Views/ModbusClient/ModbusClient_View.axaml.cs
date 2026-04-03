using Avalonia.Controls;
using Avalonia.Interactivity;
using ViewModels.ModbusClient;

namespace CoreBus.Base.Views.ModbusClient;

public partial class ModbusClient_View : UserControl
{
    public ModbusClient_View()
    {
        InitializeComponent();
    }

    private async void MonitoringSwitch_OnClick(object? sender, RoutedEventArgs e)
    {
        if (sender is not ToggleSwitch toggle || DataContext is not ModbusClient_VM viewModel)
            return;

        // Значение, которое хочет установить пользователь
        bool requestedValue = toggle.IsChecked == true;

        // Синхронно откатываем контрол обратно ДО следующего рендера — анимации нет
        toggle.IsChecked = !requestedValue;

        // Выполняем команду; если она завершится успехом,
        // VM обновит IsMonitoringMode и биндинг запустит анимацию
        await viewModel.SwitchToMonitoringMode(requestedValue);
    }
}
