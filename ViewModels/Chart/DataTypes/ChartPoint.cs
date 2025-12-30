namespace ViewModels.Chart.DataTypes;

public struct ChartPoint
{
    public Guid AxisId { get; set; }
    public double X { get; set; }
    public double Y { get; set; }

    public ChartPoint(Guid axisId, double x, double y)
    {
        AxisId = axisId;
        X = x;
        Y = y;
    }
}
