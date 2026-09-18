using Core.Models.Modbus.DataTypes;
using System.Text;

namespace Core.Models.Modbus.Message;

public class ModbusASCII_Message : ModbusMessage
{
    public override string ProtocolName { get; } = "Modbus ASCII";

    public override byte[] CreateRequest(ModbusFunction function, MessageData data, ILocalizationService localization)
    {
        var PDU = Modbus_PDU.Create(function, data, localization);

        // В этом массиве содержится SlaveID + PDU
        var mainPart = new byte[1 + PDU.Length];

        // Slave ID
        mainPart[0] = data.SlaveID;

        Array.Copy(PDU, 0, mainPart, 1, PDU.Length);

        var mainPart_ASCII = ConvertArrayToASCII(mainPart);

        var TX = data.CheckSum_IsEnable 
            ? new byte[5 + mainPart_ASCII.Length]
            : new byte[3 + mainPart_ASCII.Length];

        // Символ начала кадра (префикс)
        TX[0] = 0x3A;

        Array.Copy(mainPart_ASCII, 0, TX, 1, mainPart_ASCII.Length);

        // LRC8
        if (data.CheckSum_IsEnable)
        {
            var LRC8 = CheckSum.Calculate_LRC8(mainPart);
            
            TX[TX.Length - 4] = LRC8[0];
            TX[TX.Length - 3] = LRC8[1];
        }

        // Символы конца кадра
        TX[TX.Length - 2] = 0x0D;  // Предпоследний элемент
        TX[TX.Length - 1] = 0x0A;  // Последний элемент

        return TX;
    }

    public override ModbusResponse DecodingResponse(ModbusFunction currentFunction, byte[] sourceArray, bool checkSumIsEnable, ILocalizationService localization)
    {
        var sizeOfArray = 0;

        for (var i = 0; i < sourceArray.Length; i++)
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

        var splitArray = new byte[sizeOfArray];

        Array.Copy(sourceArray, 0, splitArray, 0, sizeOfArray);

        var mainPart = new byte[splitArray.Length - 5];

        Array.Copy(splitArray, 1, mainPart, 0, mainPart.Length);

        var convertedArray = ConvertArrayToBytes(mainPart);

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
        var ASCII_Array = new char[arrayBytes.Length * 2];

        string element;

        for (var i = 0; i < arrayBytes.Length; i++)
        {
            element = arrayBytes[i].ToString("X2");  // Представление двух разрядов числа в шестнацатеричном виде

            ASCII_Array[i * 2] = element.First();
            ASCII_Array[i * 2 + 1] = element.Last();
        }

        return Encoding.ASCII.GetBytes(ASCII_Array);
    }

    public static byte[] ConvertArrayToBytes(byte[] array)
    {
        var arrayChars = Encoding.ASCII.GetChars(array);

        var arrayJoinChars = new string[arrayChars.Length / 2];

        // В Modbus ASCII один байт представлен двумя ASCII символами
        for (var i = 0; i < arrayJoinChars.Length; i++)
        {
            arrayJoinChars[i] = string.Concat(arrayChars[i * 2], arrayChars[i * 2 + 1]);
        }

        var arrayBytes = arrayJoinChars.Where(x => x != null)
            .Select(x => byte.Parse(x, System.Globalization.NumberStyles.HexNumber)).ToArray();

        return arrayBytes;
    }
}