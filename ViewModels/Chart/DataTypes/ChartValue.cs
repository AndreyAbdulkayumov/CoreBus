namespace ViewModels.Chart.DataTypes;

public struct ChartValue
{
    public Guid AxisId { get; }
    public double Value { get; }    

    public ChartValue(Guid axisId, double value)
    {
        AxisId = axisId;
        Value = value;
    }
}
