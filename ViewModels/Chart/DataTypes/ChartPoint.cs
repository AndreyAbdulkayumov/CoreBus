namespace ViewModels.Chart.DataTypes;

public struct ChartPoint
{
    public Guid AxisId { get; }
    public double Value { get; }
    public uint IncrementX { get; }

    public ChartPoint(Guid axisId, double value, uint incrementX)
    {
        AxisId = axisId;
        Value = value;
        IncrementX = incrementX;
    }
}
