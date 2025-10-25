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
using ViewModels;
using ViewModels.Macros;
using ViewModels.Macros.MacrosEdit;
using ViewModels.ModbusScanner;
using ViewModels.Settings;

namespace CoreBus.Base.Services;

public class OpenChildWindowService : IOpenChildWindowService
{
    private Settings_VM? _settingsVM;
    private AboutApp_VM? _aboutAppVM;
    private Macros_VM? _macrosVM;
    private EditMacros_VM? _editMacrosVM;
    private ModbusScanner_VM? _modbusScannerVM;

    private const double WorkspaceOpacity_OpenChildWindow = 0.15;

    private static bool _macrosWindowIsOpen = false;

    private readonly IServiceProvider _serviceProvider;

    public OpenChildWindowService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    }

    public async Task Settings()
    {
        _settingsVM = _serviceProvider.GetService<Settings_VM>();

        if (_settingsVM == null)
        {
            return;
        }

        var window = new SettingsWindow()
        {
            DataContext = _settingsVM
        };

        window.Loaded += SettingsWindow_Loaded;
        window.KeyDown += SettingsWindow_KeyDown;

        await OpenWindowWithDimmer(window, MainWindow.Instance, MainWindow.Workspace);

        window.Loaded -= SettingsWindow_Loaded;
        window.KeyDown -= SettingsWindow_KeyDown;

        _settingsVM.WindowClosed();
    }

    private void SettingsWindow_Loaded(object? sender, RoutedEventArgs e)
    {
        _settingsVM?.WindowLoaded();
    }

    private void SettingsWindow_KeyDown(object? sender, KeyEventArgs e)
    {
        var settingsWindow = sender as SettingsWindow;

        if (settingsWindow == null)
        {
            return;
        }

        switch (e.Key)
        {
            case Key.Enter:
                _settingsVM?.Enter_KeyDownHandler();
                break;

            case Key.Escape:
                settingsWindow.Close();
                break;
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
        _aboutAppVM = _serviceProvider.GetService<AboutApp_VM>();

        if (_aboutAppVM == null)
        {
            return;
        }

        var window = new AboutWindow()
        {
            DataContext = _aboutAppVM
        };

        await OpenWindowWithDimmer(window, MainWindow.Instance, MainWindow.Workspace);
    }

    public async Task ModbusScanner()
    {
        _modbusScannerVM = _serviceProvider.GetService<ModbusScanner_VM>();

        if (_modbusScannerVM == null)
        {
            return;
        }

        var window = new ModbusScannerWindow()
        {
            DataContext = _modbusScannerVM
        };

        await OpenWindowWithDimmer(window, MainWindow.Instance, MainWindow.Workspace);

        await _modbusScannerVM.WindowClosed();
    }

    public void Macros()
    {
        try
        {
            if (_macrosWindowIsOpen)
            {
                return;
            }

            if (MainWindow.Instance == null)
            {
                throw new Exception("Не задан владелец окна.");
            }

            _macrosWindowIsOpen = true;

            _macrosVM = _serviceProvider.GetService<Macros_VM>();

            if (_macrosVM == null)
            {
                return;
            }

            var window = new MacrosWindow()
            {
                DataContext = _macrosVM
            };

            void MainWindowClosedHandler(object? sender, EventArgs e)
            {
                window?.Close();
            }

            window.Closed += (object? sender, EventArgs e) =>
            {
                MainWindow.Instance.Closed -= MainWindowClosedHandler;
                _macrosWindowIsOpen = false;
            };

            MainWindow.Instance.Closed += MainWindowClosedHandler;

            window.Show();
        }

        catch (Exception error)
        {
            _macrosWindowIsOpen = false;

            throw new Exception(error.Message);
        }
    }

    public async Task<object?> EditMacros(object? parameters)
    {
        if (MainWindow.Instance == null)
        {
            throw new Exception("Не задан владелец окна.");
        }

        await using var scope = _serviceProvider.CreateAsyncScope();

        _editMacrosVM = scope.ServiceProvider.GetRequiredService<EditMacros_VM>();

        if (_editMacrosVM == null)
        {
            return null;
        }

        // Если макрос уже существует заполняем его данными
        if (parameters != null)
        {
            _editMacrosVM.SetParameters(parameters);
        }

        var window = new EditMacrosWindow()
        {
            DataContext = _editMacrosVM
        };

        void MainWindowClosedHandler(object? sender, EventArgs e)
        {
            window?.Close();
        }

        window.Closed += (object? sender, EventArgs e) =>
        {
            MainWindow.Instance.Closed -= MainWindowClosedHandler;
        };

        MainWindow.Instance.Closed += MainWindowClosedHandler;

        await Dispatcher.UIThread.Invoke(async () =>
        {
            window.Show();
            await WaitForCloseAsync(window);
        });
        
        return _editMacrosVM.Saved ? _editMacrosVM.GetMacrosContent() : null;
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
            throw new Exception("Не задан владелец окна.");
        }

        if (workspace == null)
        {
            await window.ShowDialog(owner);
            return;
        }

        await Dispatcher.UIThread.Invoke(async () =>
        {
            double workspaceOpacity_Default = workspace.Opacity;

            workspace.Opacity = WorkspaceOpacity_OpenChildWindow;

            await window.ShowDialog(owner);

            workspace.Opacity = workspaceOpacity_Default;
        });
    }
}
