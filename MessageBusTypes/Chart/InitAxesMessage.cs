namespace MessageBusTypes.Chart;

public class InitAxesMessage
{
    public Dictionary<Guid, string> Axes { get; }
    public uint IncrementX { get; }

    public InitAxesMessage(Dictionary<Guid, string> axes, uint incrementX)
    {
        Axes = axes;
        IncrementX = incrementX;
    }
}
