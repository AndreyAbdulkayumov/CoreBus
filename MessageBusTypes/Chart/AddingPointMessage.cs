namespace MessageBusTypes.Chart;

public class AddingPointMessage
{
    public Guid AxisId { get; }
    public double X { get; }
    public double Y { get; }

    public AddingPointMessage(Guid axisId, double x, double y)
    {
        AxisId = axisId;
        X = x;
        Y = y;
    }
}
