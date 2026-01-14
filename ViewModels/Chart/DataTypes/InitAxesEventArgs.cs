namespace ViewModels.Chart.DataTypes;

public class InitAxesEventArgs
{
    public IList<ChartAxis> Axes { get; }
    public uint IncrementX { get; }

    public InitAxesEventArgs(IList<ChartAxis> axes, uint incrementX)
    {
        Axes = axes;
        IncrementX = incrementX;
    }
}
