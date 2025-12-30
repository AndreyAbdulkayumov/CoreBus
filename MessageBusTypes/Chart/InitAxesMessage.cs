namespace MessageBusTypes.Chart;

public class InitAxesMessage
{
    public Dictionary<Guid, string> Axes { get; }

    public InitAxesMessage(Dictionary<Guid, string> axes)
    {
        Axes = axes;
    }
}
