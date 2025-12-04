using ViewModels.Helpers.FloatNumber;
using ViewModels.ModbusClient.Manual.DataTypes;

namespace ViewModels.ModbusClient.Manual.Representations;

internal static class FloatRepresentation
{
    private const int FloatNumberBytesSize = 4;

    public static List<FloatRepresentation_ItemData>? GetData(ModbusDataDisplayed data)
    {
        List<FloatRepresentation_ItemData> items = new List<FloatRepresentation_ItemData>();

        if (data.Data == null || data.Data.Length < FloatNumberBytesSize)
        {
            return items;
        }

        byte[] bytes = data.Data;

        if (bytes.Length % 2 != 0)
        {
            Array.Resize(ref bytes, bytes.Length - 1);
        }

        int numbersOfNumbers = bytes.Length / FloatNumberBytesSize;

        byte[] temp = new byte[FloatNumberBytesSize];

        for (int i = 0; i < numbersOfNumbers; i++)
        {
            Array.Copy(bytes, i * FloatNumberBytesSize, temp, 0, FloatNumberBytesSize);

            Array.Reverse(temp); // т.к. в протоколе Modbus используется передача данных старшим байтом вперед.

            items.Add(new FloatRepresentation_ItemData()
            {
                Address = $"0x{(data.Address + (i * 2)).ToString("X4")}",
                AB_CD_View = FloatHelper.GetFloatNumberFromBytes(temp, FloatNumberFormat.AB_CD).ToString(),
                BA_DC_View = FloatHelper.GetFloatNumberFromBytes(temp, FloatNumberFormat.BA_DC).ToString(),
                CD_AB_View = FloatHelper.GetFloatNumberFromBytes(temp, FloatNumberFormat.CD_AB).ToString(),
                DC_BA_View = FloatHelper.GetFloatNumberFromBytes(temp, FloatNumberFormat.DC_BA).ToString(),
            });
        }

        return items;
    }
}
