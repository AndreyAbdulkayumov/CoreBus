using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Microsoft.Extensions.DependencyInjection;
using Services.Interfaces;
using System;
using System.Threading.Tasks;
using CoreBus.Base.Views;
using CoreBus.Base.Views.Macros;
using CoreBus.Base.Views.Macros.EditMacros;
using CoreBus.Base.Views.Settings;
using CoreBus.Base.Views.ModbusClient.Monitoring;
using CoreBus.Base.Views.ModbusScanner;
using CoreBus.Base.Views.Chart;
using ViewModels;
using ViewModels.Macros;
using ViewModels.Macros.MacrosEdit;
using ViewModels.ModbusScanner;
using ViewModels.Settings;
using ViewModels.ModbusClient.Monitoring;
using ViewModels.Chart;
using MessageBox.Core;

namespace CoreBus.Base.Services;

public class OpenChildWindowService : IOpenChildWindowService
{
    public bool ChartWindowIsOpen { get; private set; } = false;
    public bool MacrosWindowIsOpen { get; private set; } = false;

    private const double WorkspaceOpacity_OpenChildWindow = 0.15;
    
    private readonly IServiceProvider _serviceProvider;
    private readonly ILocalizationService _localization;

    public OpenChildWindowService(IServiceProvider serviceProvider, ILocalizationService localization)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _localization = localization ?? throw new ArgumentNullException(nameof(localization));
    }

    public async Task Settings()
    {
        SettingsWindow? window = null;
        
        await using var scope = _serviceProvider.CreateAsyncScope();
        
        var settingsVM = scope.ServiceProvider.GetRequiredService<Settings_VM>();

        window = new SettingsWindow()
        {
            DataContext = settingsVM
        };

        window.Loaded += SettingsWindowLoaded;
        window.KeyDown += SettingsWindowKeyDown;
        
        try
        {
            await OpenWindowWithDimmer(window, MainWindow.Instance, MainWindow.Workspace);

            settingsVM.WindowClosed();
        }

        finally
        {
            window?.Loaded -= SettingsWindowLoaded;
            window?.KeyDown -= SettingsWindowKeyDown;
        }
        
        void SettingsWindowLoaded(object? sender, RoutedEventArgs e)
        {
            settingsVM.WindowLoaded();
        }
        
        void SettingsWindowKeyDown(object? sender, KeyEventArgs e)
        {
            var settingsWindow = sender as SettingsWindow;

            if (settingsWindow == null)
            {
                return;
            }

            switch (e.Key)
            {
                case Key.Enter:
                    settingsVM?.Enter_KeyDownHandler();
                    break;

                case Key.Escape:
                    settingsWindow.Close();
                    break;
            }
        }
    }

    public async Task<string?> UserInput()
    {
        var window = new ServiceWindow();

        await OpenWindowWithDimmer(window, SettingsWindow.Instance, SettingsWindow.Workspace);

        return window.SelectedFilePath;
    }

    public async Task About()
    {
        var aboutAppVM = _serviceProvider.GetRequiredService<AboutApp_VM>();

        var window = new AboutWindow()
        {
            DataContext = aboutAppVM
        };

        await OpenWindowWithDimmer(window, MainWindow.Instance, MainWindow.Workspace);
    }

    public async Task ModbusScanner()
    {
        var modbusScannerVM = _serviceProvider.GetRequiredService<ModbusScanner_VM>();

        var window = new ModbusScannerWindow()
        {
            DataContext = modbusScannerVM
        };

        await OpenWindowWithDimmer(window, MainWindow.Instance, MainWindow.Workspace);

        await modbusScannerVM.WindowClosed();
    }

    public async Task Macros()
    {
        if (MacrosWindowIsOpen)
        {
            return;
        }
        
        try
        {
            if (MainWindow.Instance == null)
            {
                throw new Exception(_localization.Get("CoreBase.WindowOwnerNotSet"));
            }

            await using var scope = _serviceProvider.CreateAsyncScope();

            var macrosVM = scope.ServiceProvider.GetRequiredService<Macros_VM>();

            var window = new MacrosWindow()
            {
                DataContext = macrosVM
            };

            SubscribeOnMainWindowClosed(window);

            MacrosWindowIsOpen = true;

            await Dispatcher.UIThread.Invoke(async () =>
            {
                window.Show();
                await WaitForCloseAsync(window);
            });
        }

        finally
        {
            MacrosWindowIsOpen = false;
        }
    }

    public async Task<object?> EditMacros(object? parameters)
    {
        if (MainWindow.Instance == null)
        {
            throw new Exception(_localization.Get("CoreBase.WindowOwnerNotSet"));
        }

        await using var scope = _serviceProvider.CreateAsyncScope();

        var editMacrosVM = scope.ServiceProvider.GetRequiredService<EditMacros_VM>();

        // Если макрос уже существует заполняем его данными
        if (parameters != null)
        {
            editMacrosVM.SetParameters(parameters);
        }

        var window = new EditMacrosWindow()
        {
            DataContext = editMacrosVM
        };

        SubscribeOnMainWindowClosed(window);

        await Dispatcher.UIThread.Invoke(async () =>
        {
            window.Show();
            await WaitForCloseAsync(window);
        });

        return editMacrosVM.Saved ? editMacrosVM.GetMacrosContent() : null;
    }

    public async Task<string?> EditFormula(string description, string? formula, bool isEnable)
    {
        if (MainWindow.Instance == null)
        {
            throw new Exception(_localization.Get("CoreBase.WindowOwnerNotSet"));
        }

        await using var scope = _serviceProvider.CreateAsyncScope();

        var editFormulaVM = scope.ServiceProvider.GetRequiredService<EditFormula_VM>();

        editFormulaVM.InitWindow(description, formula, isEnable);

        var window = new EditFormulaWindow()
        {
            DataContext = editFormulaVM
        };

        await OpenWindowWithDimmer(window, MainWindow.Instance, MainWindow.Workspace);

        return editFormulaVM.GetResult();
    }

    public void Chart()
    {
        try
        {
            if (ChartWindowIsOpen)
            {
                return;
            }

            if (MainWindow.Instance == null)
            {
                throw new Exception(_localization.Get("CoreBase.WindowOwnerNotSet"));
            }

            var chartVM = _serviceProvider.GetRequiredService<Chart_VM>();

            var window = new ChartWindow()
            {
                DataContext = chartVM
            };

            SubscribeOnMainWindowClosed(window, () =>
            {
                ChartWindowIsOpen = false;
                window.UnsubscribeFromEvents();
            });

            window.Closing += async (object? sender, WindowClosingEventArgs e) =>
            {
                if (!ChartWindowIsOpen)
                    return;

                var monitoringVM = _serviceProvider.GetService<ModbusMonitoring_VM>();
                var chartVM = _serviceProvider.GetService<Chart_VM>();
                var chartMessageBox = _serviceProvider.GetService<IMessageBoxChart>();

                if (monitoringVM == null || chartVM == null || chartMessageBox == null)
                    return;

                if (!monitoringVM.IsMonitoringRunning)
                    return;

                // Событие ожидает синхронного выполнения обработчика, а у нас есть асинхронная операция ниже.
                // Отменять закрытие окна нужно, чтобы успеть показать MessageBox.
                // Без этого окно просто закроется, и все что ниже будет проигнорировано.
                e.Cancel = true;

                if (monitoringVM.IsMonitoringRunning)
                {
                    var closeWindow = await chartMessageBox.ShowYesNoDialog(
                        _localization.Get("Message.Confirm.CloseChartWhileMonitoring"),
                        MessageType.Warning);

                    if (closeWindow != MessageBoxResult.No)
                    {
                        ChartWindowIsOpen = false;
                        window.Close();
                    }
                }
            };

            ChartWindowIsOpen = true;

            window.Show();
        }

        catch (Exception)
        {
            ChartWindowIsOpen = false;
            throw;
        }
    }

    public void RaiseChartWindow()
    {
        ChartWindow.Instance?.Activate();
    }

    private Task WaitForCloseAsync(Window window)
    {
        var tcs = new TaskCompletionSource<object?>();

        void Handler(object? sender, EventArgs e)
        {
            window.Closed -= Handler;
            tcs.TrySetResult(null);
        }

        window.Closed += Handler;

        return tcs.Task;
    }

    private async Task OpenWindowWithDimmer(Window window, Window? owner, Visual? workspace)
    {
        if (owner == null)
        {
            throw new Exception(_localization.Get("CoreBase.WindowOwnerNotSet"));
        }

        if (workspace == null)
        {
            await window.ShowDialog(owner);
            return;
        }

        await Dispatcher.UIThread.Invoke(async () =>
        {
            var workspaceOpacityDefault = workspace.Opacity;

            workspace.Opacity = WorkspaceOpacity_OpenChildWindow;

            await window.ShowDialog(owner);

            workspace.Opacity = workspaceOpacityDefault;
        });
    }
    
    private void SubscribeOnMainWindowClosed(Window window, Action? closeHandler = null)
    {
        var mainWindow = MainWindow.Instance;
        if (mainWindow == null)
            return;
        
        window.Closed += (object? sender, EventArgs e) =>
        {
            mainWindow.Closed -= MainWindowClosedHandler;
            closeHandler?.Invoke();
        };

        mainWindow.Closed += MainWindowClosedHandler;

        void MainWindowClosedHandler(object? sender, EventArgs e)
        {
            window.Close();
        }
    }
}
