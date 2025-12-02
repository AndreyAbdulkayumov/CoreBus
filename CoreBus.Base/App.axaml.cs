using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using CoreBus.Base.Views;
using CoreBus.Base.Services;
using ViewModels;
using ViewModels.NoProtocol;
using ViewModels.ModbusClient;
using ViewModels.Macros;
using ViewModels.Settings;
using ViewModels.Settings.Tabs;
using Services.Interfaces;
using ViewModels.Macros.MacrosEdit;
using Core.Models;
using Core.Models.Settings;
using Core.Models.NoProtocol;
using Core.Models.Modbus;
using Core.Models.AppUpdateSystem;
using ViewModels.ModbusScanner;

namespace CoreBus.Base;

public partial class App : Application
{
    private readonly IServiceProvider _serviceProvider;

    /// <summary>
    /// 
    /// Правила создания сервисов
    /// 
    /// 1. Все модели создаются как Singleton.
    /// 2. Главное окно и все его ViewModel создаются как Singleton.
    /// 3. Дочерние окна и все их элементы создаются как Transient. 
    /// 4. Дочерние окна, которые используют MessageBus, должны создаваться как Scoped и реализовывать интерфейс IDisposable.
    /// Это нужно для корректной отписки от события и вызова метода Dispose. В Transient метод Dispose не вызывается.
    /// 5. Экземпляр ViewModel дочернего окна создается в OpenChildWindowService через _serviceProvider.GetService<T>().
    /// 
    /// 3, 4, 5 правила нужны для того, чтобы экземпляры ViewModel дочерних окон уничтожались после закрытия окна.
    /// При каждом открытии нового окна создаются новые экземпляры ViewModel.
    /// 
    /// 6. Вспомогательные сервисы и сервисы MessageBox создаются как Singleton, чтобы не создавалось куча ненужных экземпляров.
    /// 
    /// </summary>
    public App()
    {
        var serviceCollection = new ServiceCollection()
            // Модели
            .AddSingleton<ConnectedHost>()
            .AddSingleton<Model_NoProtocol>()
            .AddSingleton<Model_Modbus>()
            .AddSingleton<Model_Settings>()
            .AddSingleton<Model_AppUpdateSystem>()
            // Главное окно
            .AddSingleton<MainWindow_VM>()
            // Компоненты главного окна
            .AddSingleton<NoProtocol_VM>()
            .AddSingleton<NoProtocol_Mode_Normal_VM>()
            .AddSingleton<NoProtocol_Mode_Cycle_VM>()
            .AddSingleton<NoProtocol_Mode_Files_VM>()
            .AddSingleton<ModbusClient_VM>()
            .AddSingleton<ModbusClient_Mode_Normal_VM>()
            // Окно настроек
            .AddTransient<Settings_VM>()
            // Компоненты окна настроек
            .AddTransient<AppSettings_VM>()
            .AddTransient<Connection_Ethernet_VM>()
            .AddTransient<Connection_SerialPort_VM>()
            .AddTransient<Connection_VM>()
            .AddTransient<Modbus_VM>()
            .AddTransient<Settings_NoProtocol_VM>()
            // Окно "О программе"
            .AddTransient<AboutApp_VM>()
            // Окно Modbus сканнера
            .AddTransient<ModbusScanner_VM>()
            // Окно макросов
            .AddScoped<Macros_VM>()
            // Окно редактирования макроса
            .AddScoped<EditMacros_VM>()
            // MessageBox с разными владельцами
            .AddSingleton<IMessageBoxMainWindow, MessageBoxMainWindow>()
            .AddSingleton<IMessageBoxSettings, MessageBoxSettings>()
            .AddSingleton<IMessageBoxMacros, MessageBoxMacros>()
            .AddSingleton<IMessageBoxEditMacros, MessageBoxEditMacros>()
            .AddSingleton<IMessageBoxModbusScanner, MessageBoxModbusScanner>()
            .AddSingleton<IMessageBoxAboutApp, MessageBoxAboutApp>()
            // Вспомогательные сервисы
            .AddSingleton<IUIService, UIService>()
            .AddSingleton<IFileSystemService, FileSystemService>()
            .AddSingleton<IOpenChildWindowService, OpenChildWindowService>();

        _serviceProvider = serviceCollection.BuildServiceProvider();
    }

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow()
            {
                DataContext = _serviceProvider.GetService<MainWindow_VM>(),
            };

            desktop.MainWindow.Loaded += (object? sender, RoutedEventArgs e) =>
            {
                (desktop.MainWindow.DataContext as MainWindow_VM)?.MainWindowLoaded();
            };

            desktop.MainWindow.Closing += (object? sender, WindowClosingEventArgs e) =>
            {
                (desktop.MainWindow.DataContext as MainWindow_VM)?.WindowClosing();
            };
        }

        base.OnFrameworkInitializationCompleted();
    }
}
