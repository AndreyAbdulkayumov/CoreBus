namespace ViewModels.ModbusClient.Manual.WriteFields.DataTypes;

public class RequestToUpdateAddressesArgs : EventArgs
{
    public readonly Guid ItemId;
    public readonly string NewFormat;

    public RequestToUpdateAddressesArgs(Guid itemId, string newFormat)
    {
        ItemId = itemId;
        NewFormat = newFormat;
    }
}
