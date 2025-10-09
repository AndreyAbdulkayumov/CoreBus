namespace Core.Tests.Modbus;

public static class ModbusField
{
    public static byte[] Get_Address(UInt16 Address)
    {
        byte[] ArrayBytes = BitConverter.GetBytes(Address);

        if (ArrayBytes.Length != 2)
        {
            throw new Exception("Поле адреса имеет недопустимое количество байт: " + ArrayBytes.Length);
        }

        return ArrayBytes;
    }

    public static byte[] Get_NumberOfRegisters(UInt16 NumberOfRegisters)
    {
        byte[] ArrayBytes = BitConverter.GetBytes(NumberOfRegisters);

        if (ArrayBytes.Length != 2)
        {
            throw new Exception("Поле количества регистров имеет недопустимое количество байт: " + ArrayBytes.Length);
        }

        return ArrayBytes;
    }

    public static byte[] Get_WriteData(UInt16[] Data)
    {
        var ListBytes = new List<byte>();

        byte[] Temp;

        foreach (UInt16 element in Data)
        {
            Temp = BitConverter.GetBytes(element);

            ListBytes.Add(Temp[1]);
            ListBytes.Add(Temp[0]);
        }

        if (ListBytes.Count != Data.Length * 2)
        {
            throw new Exception("Поле данных имеет недопустимое количество байт: " + ListBytes.Count);
        }

        return ListBytes.ToArray();
    }

    public static (byte[], int) Get_WriteDataFromMultipleCoils(int[] bitArray)
    {
        var result = new List<byte>();

        byte temp = 0;

        for (int i = 0; i < bitArray.Length; i++)
        {
            temp |= (byte)(bitArray[i] << (i % 8));

            if ((i + 1) % 8 == 0)
            {
                result.Add(temp);
                temp = 0;
            }
        }

        if (result.Count == 0 || temp != 0)
        {
            result.Add(temp);
        }

        return (result.ToArray(), bitArray.Length);
    }
}
