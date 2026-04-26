using Core.Models.Modbus.DataTypes;
using Services.Interfaces;
using System.Text;

namespace Core.Models.Modbus.Message;

public class ModbusASCII_Message : ModbusMessage
{
    public override string ProtocolName { get; } = "Modbus ASCII";

    public override byte[] CreateMessage(ModbusFunction function, MessageData data, ILocalizationService localization)
    {
        byte[] PDU = Modbus_PDU.Create(function, data, localization);

        // В этом массиве содержится SlaveID + PDU
        byte[] mainPart = new byte[1 + PDU.Length];

        // Slave ID
        mainPart[0] = data.SlaveID;

        Array.Copy(PDU, 0, mainPart, 1, PDU.Length);

        byte[] mainPart_ASCII = ConvertArrayToASCII(mainPart);

        byte[] TX;

        if (data.CheckSum_IsEnable)
        {
            TX = new byte[5 + mainPart_ASCII.Length];
        }

        else
        {
            TX = new byte[3 + mainPart_ASCII.Length];
        }

        // Символ начала кадра (префикс)
        TX[0] = 0x3A;

        Array.Copy(mainPart_ASCII, 0, TX, 1, mainPart_ASCII.Length);

        // LRC8
        if (data.CheckSum_IsEnable)
        {
            byte[] LRC8 = CheckSum.Calculate_LRC8(mainPart);
            TX[TX.Length - 4] = LRC8[0];
            TX[TX.Length - 3] = LRC8[1];
        }

        // Символы конца кадра
        TX[TX.Length - 2] = 0x0D;  // Предпоследний элемент
        TX[TX.Length - 1] = 0x0A;  // Последний элемент

        return TX;
    }

    public override ModbusResponse DecodingMessage(ModbusFunction currentFunction, byte[] sourceArray, ILocalizationService localization)
    {
        int sizeOfArray = 0;

        for (int i = 0; i < sourceArray.Length; i++)
        {
            if (i + 1 <= sourceArray.Length)
            {
                // Спец. символы 0x0D и 0x0A встречаются только в конце значимой части массива
                if (sourceArray[i] == 0x0D && sourceArray[i + 1] == 0x0A)
                {
                    sizeOfArray = i + 2;
                    break;
                }
            }

            else
            {
                sizeOfArray = sourceArray.Length;
                break;
            }
        }

        byte[] splitArray = new byte[sizeOfArray];

        Array.Copy(sourceArray, 0, splitArray, 0, sizeOfArray);

        byte[] mainPart = new byte[splitArray.Length - 5];

        Array.Copy(splitArray, 1, mainPart, 0, mainPart.Length);

        byte[] convertedArray = ConvertArrayToBytes(mainPart);

        var decodingResponse = new ModbusResponse
        {
            SlaveID = convertedArray[0],
            Command = convertedArray[1]
        };

        CheckErrorCode(TypeOfModbus.ASCII, ref decodingResponse, convertedArray, localization);

        if (currentFunction is ModbusReadFunction)
        {
            decodingResponse.LengthOfData = convertedArray[2];

            if (decodingResponse.LengthOfData == 0)
            {
                throw new Exception(localization.Get("Core.Modbus.EmptyDataPartAscii", currentFunction.Number));
            }

            decodingResponse.Data = new byte[decodingResponse.LengthOfData];

            // Согласно документации на протокол Modbus:
            // В ответном пакете Modbus ASCII на команды чтения
            // информационная часть начинается с 3 байта.

            Array.Copy(convertedArray, 3, decodingResponse.Data, 0, decodingResponse.LengthOfData);

            // Реверс байтов не нужен функциям, работающими с флагами (номера 1 и 2).
            if (currentFunction != Function.ReadCoilStatus &&
                currentFunction != Function.ReadDiscreteInputs)
            {
                decodingResponse.Data = ReverseLowAndHighBytesInWords(decodingResponse.Data);
            }
        }

        else if (currentFunction is ModbusWriteFunction)
        {
            decodingResponse.LengthOfData = -1;
        }

        else
        {
            throw new Exception(localization.Get("Core.Modbus.UnsupportedCommandCode", currentFunction.Number));
        }

        return decodingResponse;
    }

    public static byte[] ConvertArrayToASCII(byte[] arrayBytes)
    {
        // В Modbus ASCII один байт представлен двумя ASCII символами
        char[] ASCII_Array = new char[arrayBytes.Length * 2];

        string element;

        for (int i = 0; i < arrayBytes.Length; i++)
        {
            element = arrayBytes[i].ToString("X2");  // Представление двух разрядов числа в шестнацатеричном виде

            ASCII_Array[i * 2] = element.First();
            ASCII_Array[i * 2 + 1] = element.Last();
        }

        return Encoding.ASCII.GetBytes(ASCII_Array);
    }

    public static byte[] ConvertArrayToBytes(byte[] array)
    {
        char[] arrayChars = Encoding.ASCII.GetChars(array);

        string[] arrayJoinChars = new string[arrayChars.Length / 2];

        // В Modbus ASCII один байт представлен двумя ASCII символами
        for (int i = 0; i < arrayJoinChars.Length; i++)
        {
            arrayJoinChars[i] = string.Concat(arrayChars[i * 2], arrayChars[i * 2 + 1]);
        }

        byte[] arrayBytes = arrayJoinChars.Where(x => x != null)
            .Select(x => byte.Parse(x, System.Globalization.NumberStyles.HexNumber)).ToArray();

        return arrayBytes;
    }
}