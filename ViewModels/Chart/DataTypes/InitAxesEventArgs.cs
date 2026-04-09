namespace ViewModels.Chart.DataTypes;

public class InitAxesEventArgs
{
    public IEnumerable<ChartAxis> Axes { get; }
    public uint IncrementX { get; }

    public InitAxesEventArgs(IEnumerable<ChartAxis> axes, uint incrementX)
    {
        Axes = axes;
        IncrementX = incrementX;
    }
}
