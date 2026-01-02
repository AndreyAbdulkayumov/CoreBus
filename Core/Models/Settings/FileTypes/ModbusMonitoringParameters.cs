using System.Globalization;

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

public class ModbusMonitoringParameters
{
    public byte SlaveID { get; set; }
    public int FunctionNumber { get; set; }
    public uint Period { get; set; }
    public NumberStyles NumberStyle { get; set; }
    public List<ModbusMonitoringItemData>? Items { get; set; }

    public static ModbusMonitoringParameters GetDefault()
    {
        return new ModbusMonitoringParameters()
        {
            SlaveID = 7,
            FunctionNumber = 4,
            Period = 600,
            NumberStyle = NumberStyles.Number,
            Items = null,
        };
    }
}
