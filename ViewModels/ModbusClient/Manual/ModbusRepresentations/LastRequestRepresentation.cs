using ViewModels.ModbusClient.Manual.DataTypes;

namespace ViewModels.ModbusClient.Manual.ModbusRepresentations;

internal static class LastRequestRepresentation
{
    public static IEnumerable<RequestResponseField_ItemData> GetData(string[] requestBytes, string[] responseBytes)
    {
        int maxLength = requestBytes.Length > responseBytes.Length ? requestBytes.Length : responseBytes.Length;

        var items = new RequestResponseField_ItemData[maxLength];

        for (int i = 0; i < items.Length; i++)
        {
            items[i] = new RequestResponseField_ItemData
            {
                ItemNumber = (i + 1).ToString()
            };
        }

        for (int i = 0; i < requestBytes.Length; i++)
        {
            items[i].RequestData = requestBytes[i];
        }

        for (int i = 0; i < responseBytes.Length; i++)
        {
            items[i].ResponseData = responseBytes[i];
        }

        return items;
    }
}
