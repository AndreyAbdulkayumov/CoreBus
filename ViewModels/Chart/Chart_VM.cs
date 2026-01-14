using MessageBusTypes.Chart;
using ReactiveUI;
using System.Reactive.Disposables;
using ViewModels.Chart.DataTypes;

namespace ViewModels.Chart;

public class Chart_VM : ReactiveObject, IDisposable
{
    // Чтобы исключить работу с UI компонентами управление графиком происходит через события
    public static event EventHandler<InitAxesEventArgs>? InitAxes;
    public static event EventHandler<ChartValue>? AddPointOnChart;

    private readonly CompositeDisposable _disposables = new CompositeDisposable();

    public Chart_VM()
    {
        /****************************************************/
        //
        // Настройка прослушивания MessageBus
        //
        /****************************************************/

        _disposables.Add(
            MessageBus.Current.Listen<InitAxesMessage>()
                .Subscribe(message =>
                {
                    var args = new InitAxesEventArgs(
                        message.Axes.Select(e => new ChartAxis(e.Key, e.Value)).ToList(), 
                        message.IncrementX);

                    InitAxes?.Invoke(this, args);
                }));

        _disposables.Add(
            MessageBus.Current.Listen<AddingPointMessage>()
                .Subscribe(message =>
                {
                    AddPointOnChart?.Invoke(this, new ChartValue(message.AxisId, message.Value));
                }));
    }

    public void Dispose()
    {
        _disposables.Dispose();
    }
}
