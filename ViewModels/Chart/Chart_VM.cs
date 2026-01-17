using MessageBusTypes.Chart;
using ReactiveUI;
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

    private uint _numberOfVisiblePoints = 10;

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

    public Chart_VM()
    {
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
    }
}
