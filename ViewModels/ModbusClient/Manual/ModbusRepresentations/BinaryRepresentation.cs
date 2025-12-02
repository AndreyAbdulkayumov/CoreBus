using MessageBox.Core;
using ViewModels.ModbusClient.Manual.DataTypes;

namespace ViewModels.ModbusClient.Manual.ModbusRepresentations;

internal static class BinaryRepresentation
{
    public static IEnumerable<BinaryRepresentation_ItemData>? GetData(ModbusDataDisplayed data,
        IMessageBox messageBox, Func<string, Task> copyToClipboard)
    {
        if (data.Data == null)
            return null;

        byte[] bytes;

        if (data.Data.Length % 2 != 0)
        {
            bytes = data.Data.Concat(new byte[] { 0 }).ToArray();
        }

        else
        {
            bytes = data.Data;
        }

        UInt16[] words = new UInt16[bytes.Length / 2];

        for (int i = 0; i < words.Length; i++)
        {
            words[i] = BitConverter.ToUInt16(bytes, i * 2);
        }

        var binaryWords = words.Select(e => Convert.ToString(e, 2).PadLeft(16, '0'));

        if (binaryWords == null)
            return null;

        ushort currentAddress = data.Address;

        var items = new List<BinaryRepresentation_ItemData>();

        foreach (string element in binaryWords)
        {
            items.Add(GetBinaryRepresentation(currentAddress, element, messageBox, copyToClipboard));
            currentAddress += 1;
        }

        return items;
    }

    private static BinaryRepresentation_ItemData GetBinaryRepresentation(ushort address, string inputData,
        IMessageBox messageBox, Func<string, Task> copyToClipboard)
    {
        char[] bitsValue = inputData.ToCharArray();

        var itemGroup = new List<BinaryDataItemGroup>();

        var items = new List<BinaryDataItem>();

        for (int i = 0; i < bitsValue.Length; i++)
        {
            var item = new BinaryDataItem()
            {
                Bit = bitsValue[i].ToString(),
                IsZeroBit = bitsValue[i] == '0'
            };

            items.Add(item);

            if ((i + 1) % 4 == 0)
            {
                itemGroup.Add(new BinaryDataItemGroup(items.ToArray()));
                items.Clear();
            }
        }

        return new BinaryRepresentation_ItemData(
            address: "0x" + address.ToString("X2"),
            binaryData: itemGroup.ToArray(),
            messageBox,
            copyToClipboard);
    }
}
