namespace MessageBusTypes.Chart;

public class InitAxesMessage
{
    public Dictionary<Guid, string> Axes { get; }
    public uint IncrementX { get; }
    public bool IsStart { get; }

    public InitAxesMessage(Dictionary<Guid, string> axes, uint incrementX, bool isStart)
    {
        Axes = axes;
        IncrementX = incrementX;
        IsStart = isStart;
    }
}
