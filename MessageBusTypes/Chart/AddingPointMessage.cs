namespace MessageBusTypes.Chart;

public class AddingPointMessage
{
    public Guid AxisId { get; }
    public double Value { get; }

    public AddingPointMessage(Guid axisId, double value)
    {
        AxisId = axisId;
        Value = value;
    }
}
