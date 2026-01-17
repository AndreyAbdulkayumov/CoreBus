namespace MessageBusTypes.Chart;

public class ManageChartToolsMessage
{
    public bool ToolsIsEnabled { get; }

    public ManageChartToolsMessage(bool toolsIsEnabled)
    {
        ToolsIsEnabled = toolsIsEnabled;
    }
}
