using Core.Models.Settings;
using Core.Models.Settings.FileTypes;
using MessageBox.Core;
using MessageBusTypes.Chart;
using ReactiveUI;
using Services.Interfaces;
using ViewModels.Chart.DataTypes;

namespace ViewModels.Chart;

public class Chart_VM : ReactiveObject
{
    // Чтобы исключить работу с UI компонентами управление графиком происходит через события
    public static event EventHandler<InitAxesEventArgs>? InitAxes;
    public static event EventHandler<ChartValue>? AddPointOnChart;

    private bool _windowIsTopmost;

    public bool WindowIsTopmost
    {
        get => _windowIsTopmost;
        set => this.RaiseAndSetIfChanged(ref  _windowIsTopmost, value);
    }

    private uint _numberOfVisiblePoints;

    public uint NumberOfVisiblePoints
    {
        get => _numberOfVisiblePoints;
        set => this.RaiseAndSetIfChanged(ref _numberOfVisiblePoints, value);
    }

    private bool _toolsIsEnabled = true;

    public bool ToolsIsEnabled
    {
        get => _toolsIsEnabled;
        set => this.RaiseAndSetIfChanged(ref _toolsIsEnabled, value);
    }

    private bool _isConverted;

    public bool IsConverted
    {
        get => _isConverted;
        set => this.RaiseAndSetIfChanged(ref _isConverted, value);
    }

    private readonly Model_Settings _settingsModel;
    private readonly IFileSystemService _fileSystemService;
    private readonly IMessageBoxChart _messageBox;
    

    public Chart_VM(Model_Settings settingsModel, IFileSystemService fileSystemService, IMessageBoxChart messageBox)
    {
        _settingsModel = settingsModel ?? throw new ArgumentNullException(nameof(settingsModel));
        _fileSystemService = fileSystemService ?? throw new ArgumentNullException(nameof(fileSystemService));
        _messageBox = messageBox ?? throw new ArgumentNullException(nameof(messageBox));

        /****************************************************/
        //
        // Настройка прослушивания MessageBus
        //
        /****************************************************/

        MessageBus.Current.Listen<InitAxesMessage>()
            .Subscribe(message =>
            {
                var args = new InitAxesEventArgs(
                    message.Axes.Select(e => new ChartAxis(e.Key, e.Value)).ToList(),
                    message.IncrementX);

                InitAxes?.Invoke(this, args);
            });

        MessageBus.Current.Listen<AddingPointMessage>()
            .Subscribe(message =>
            {
                AddPointOnChart?.Invoke(this, new ChartValue(message.AxisId, message.Value));
            });

        MessageBus.Current.Listen<ManageChartToolsMessage>()
            .Subscribe(message =>
            {
                ToolsIsEnabled = message.ToolsIsEnabled;
            });

        // Действия после запуска приложения

        SetParameters(_settingsModel.ModbusMonitoringItems);
    }

    public void SetParameters(ModbusMonitoringParameters data)
    {
        NumberOfVisiblePoints = data.ChartInfo != null ? data.ChartInfo.NumberOfVisiblePoints : 10;
        WindowIsTopmost = data.ChartInfo != null ? data.ChartInfo.ChartIsTopmost : false;
    }

    public async Task<string?> GetFilePath()
    {
        var path = await _fileSystemService.GetFolderPath("Выгрузка точек графика");

        if (path == null) 
            return null;

        return Path.Combine(path, $"CoreBus {DateTime.Now:dd.MM.yyyy HH.mm.ss}.txt");
    }

    public void ShowMessage(string message, Exception? error = null, bool isInfo = false)
    {
        if (error != null)
        {
            _messageBox.Show(message, MessageType.Error, error);
            return;
        }

        _messageBox.Show(message, isInfo ? MessageType.Information : MessageType.Warning);
    }
}
