using System.Globalization;

namespace Core.Models.Settings.FileTypes;

public enum MonitoringValueType
{
    UInt16,
    Int16,
    UInt32,
    Int32,
    Float,
}

public class MonitoringChart : ICloneable
{
    public uint NumberOfVisiblePoints { get; set; }
    public bool ChartIsTopmost { get; set; }

    public object Clone()
    {
        return new MonitoringChart()
        {
            NumberOfVisiblePoints = NumberOfVisiblePoints,
            ChartIsTopmost = ChartIsTopmost
        };
    }
}

public class ModbusMonitoringItemData : ICloneable
{
    public UInt16 Address { get; set; }
    public string? Alias { get; set; }
    public MonitoringValueType ValueType { get; set; }
    public bool VisibleOnlyRawValue { get; set; }
    public string? Formula { get; set; }
    public bool ShowOnChartAndLog { get; set; }

    public object Clone()
    {
        return new ModbusMonitoringItemData()
        {
            Address = Address,
            Alias = Alias,
            ValueType = ValueType,
            VisibleOnlyRawValue = VisibleOnlyRawValue,
            Formula = Formula,
            ShowOnChartAndLog = ShowOnChartAndLog
        };
    }
}

public class ModbusMonitoringParameters
{
    public byte SlaveID { get; set; }
    public int FunctionNumber { get; set; }
    public uint Period { get; set; }
    public NumberStyles NumberStyle { get; set; }
    public MonitoringChart? ChartInfo { get; set; }
    public List<ModbusMonitoringItemData>? Items { get; set; }

    public static ModbusMonitoringParameters GetDefault()
    {
        return new ModbusMonitoringParameters()
        {
            SlaveID = 7,
            FunctionNumber = 4,
            Period = 600,
            NumberStyle = NumberStyles.Number,
            ChartInfo = new MonitoringChart()
            {
                NumberOfVisiblePoints = 10,
                ChartIsTopmost = false
            },
            Items = null,
        };
    }
}
