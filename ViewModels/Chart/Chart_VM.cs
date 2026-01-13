using MessageBusTypes.Chart;
using ReactiveUI;
using System.Reactive.Disposables;
using ViewModels.Chart.DataTypes;

namespace ViewModels.Chart;

public class Chart_VM : ReactiveObject, IDisposable
{
    // Чтобы исключить работу с UI компонентами управление графиком происходит через события
    public static event EventHandler<IList<ChartAxis>>? InitAxis;
    public static event EventHandler<ChartPoint>? AddPointOnChart;

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
                    var axes = message.Axes.Select(e => new ChartAxis(e.Key, e.Value)).ToList();

                    InitAxis?.Invoke(this, axes);
                }));

        _disposables.Add(
            MessageBus.Current.Listen<AddingPointMessage>()
                .Subscribe(message =>
                {
                    AddPointOnChart?.Invoke(this, new ChartPoint(message.AxisId, message.Value, message.IncrementX));
                }));
    }

    public void Dispose()
    {
        _disposables.Dispose();
    }
}
