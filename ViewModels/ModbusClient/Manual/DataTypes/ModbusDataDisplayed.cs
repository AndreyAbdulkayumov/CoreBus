namespace ViewModels.ModbusClient.Manual.DataTypes;

public class ModbusDataDisplayed
{
    public ushort OperationID { get; set; }
    public string? FuncNumber { get; set; }
    public ushort Address { get; set; }
    public string? ViewAddress { get; set; }
    public byte[]? Data { get; set; }
    public string? ViewData { get; set; }
}
