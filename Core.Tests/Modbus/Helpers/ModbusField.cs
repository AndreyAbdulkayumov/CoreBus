namespace Core.Tests.Modbus.Helpers;

public static class ModbusField
{
    public static byte[] Get_Address(UInt16 address)
    {
        var arrayBytes = BitConverter.GetBytes(address);

        if (arrayBytes.Length != 2)
        {
            throw new Exception("Поле адреса имеет недопустимое количество байт: " + arrayBytes.Length);
        }

        return arrayBytes;
    }

    public static byte[] Get_NumberOfRegisters(UInt16 numberOfRegisters)
    {
        var arrayBytes = BitConverter.GetBytes(numberOfRegisters);

        if (arrayBytes.Length != 2)
        {
            throw new Exception("Поле количества регистров имеет недопустимое количество байт: " + arrayBytes.Length);
        }

        return arrayBytes;
    }

    public static byte[] Get_WriteData(UInt16[] data)
    {
        var listBytes = new List<byte>();

        foreach (var element in data)
        {
            var temp = BitConverter.GetBytes(element);

            listBytes.Add(temp[1]);
            listBytes.Add(temp[0]);
        }

        if (listBytes.Count != data.Length * 2)
        {
            throw new Exception("Поле данных имеет недопустимое количество байт: " + listBytes.Count);
        }

        return listBytes.ToArray();
    }

    public static (byte[], int) Get_WriteDataFromMultipleCoils(int[] bitArray)
    {
        var result = new List<byte>();

        byte temp = 0;

        for (var i = 0; i < bitArray.Length; i++)
        {
            temp |= (byte)(bitArray[i] << (i % 8));

            if ((i + 1) % 8 == 0)
            {
                result.Add(temp);
                temp = 0;
            }
        }

        if (result.Count * 8 < bitArray.Length)
        {
            result.Add(temp);
        }

        return (result.ToArray(), bitArray.Length);
    }
}
