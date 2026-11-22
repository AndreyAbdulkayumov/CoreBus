using System.Collections.ObjectModel;

namespace MessageBusTypes.Chart;

public class InitAxesMessage
{
    public IReadOnlyDictionary<Guid, string> Axes { get; }
    public uint IncrementX { get; }

    public InitAxesMessage(Dictionary<Guid, string> axes, uint incrementX)
    {
        Axes = new ReadOnlyDictionary<Guid, string>(axes);
        IncrementX = incrementX;
    }
}
