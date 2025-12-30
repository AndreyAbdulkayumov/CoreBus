namespace Core.Models.Settings.FileTypes;

public class ModbusMonitoringItemData : ICloneable
{
    public UInt16 Address { get; set; }
    public string? Alias { get; set; }
    public string? ValueType { get; set; }
    public bool VisibleOnlyRawValue { get; set; }
    public bool OnChart { get; set; }

    public object Clone()
    {
        return new ModbusMonitoringItemData()
        {
            Address = Address,
            Alias = Alias,
            ValueType = ValueType,
            VisibleOnlyRawValue = VisibleOnlyRawValue,
            OnChart = OnChart
        };
    }
}

public class ModbusMonitoring
{
    public List<ModbusMonitoringItemData>? Items { get; set; }
}
