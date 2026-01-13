namespace MessageBusTypes.Chart;

public class AddingPointMessage
{
    public Guid AxisId { get; }
    public double Value { get; }
    public uint IncrementX { get; }

    public AddingPointMessage(Guid axisId, double value, uint incrementX)
    {
        AxisId = axisId;
        Value = value;
        IncrementX = incrementX;
    }
}
