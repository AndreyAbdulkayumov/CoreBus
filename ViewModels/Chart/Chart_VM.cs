using MessageBusTypes.Chart;
using ReactiveUI;
using Services.Interfaces;
using ViewModels.Chart.DataTypes;

namespace ViewModels.Chart;

public class Chart_VM : ReactiveObject
{
    // Чтобы исключить работу с UI компонентами управление графиком происходит через события
    public static event EventHandler<IList<ChartAxis>>? InitAxis;
    public static event EventHandler<ChartPoint>? AddPointOnChart;

    public Chart_VM(IMessageBoxChart messageBox)
    {
        /****************************************************/
        //
        // Настройка прослушивания MessageBus
        //
        /****************************************************/

        MessageBus.Current.Listen<InitAxesMessage>()
            .Subscribe(message =>
            {
                var axes = message.Axes.Select(e => new ChartAxis(e.Key, e.Value)).ToList();

                InitAxis?.Invoke(this, axes);
            });

        MessageBus.Current.Listen<AddingPointMessage>()
            .Subscribe(message =>
            {
                AddPointOnChart?.Invoke(this, new ChartPoint(message.AxisId, message.X, message.Y));
            });

        /****************************************************/
        //
        // Настройка свойств и команд модели отображения
        //
        /****************************************************/

    }
}
