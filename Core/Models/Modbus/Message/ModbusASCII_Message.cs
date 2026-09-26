using Core.Models.Modbus.DataTypes;
using System.Text;

namespace Core.Models.Modbus.Message;

public class ModbusASCII_Message : ModbusMessage
{
    public override string ProtocolName { get; } = "Modbus ASCII";

    /// <summary>
    /// Первый байт сообщения Modbus ASCII (символ ':' = 0x3A)
    /// </summary>
    public static byte StartByte => 0x3A;
    
    /// <summary>
    /// Предпоследний байт сообщения Modbus ASCII (символ 'CR' = 0x0D)
    /// </summary>
    public static byte EndByteCR => 0x0D;
    
    /// <summary>
    /// Последний байт сообщения Modbus ASCII (символ 'LF' = 0x0A)
    /// </summary>
    public static byte EndByteLF => 0x0A;
    

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
        TX[0] = StartByte;

        Array.Copy(mainPart_ASCII, 0, TX, 1, mainPart_ASCII.Length);

        // LRC8
        if (data.CheckSum_IsEnable)
        {
            var LRC8 = CheckSum.Calculate_LRC8_ASCII(mainPart);
            
            TX[^4] = LRC8[0];
            TX[^3] = LRC8[1];
        }

        // Символы конца кадра
        TX[^2] = EndByteCR;  // Предпоследний элемент
        TX[^1] = EndByteLF;  // Последний элемент

        return TX;
    }

    public override ModbusResponse DecodingResponse(ModbusFunction currentFunction, byte[] sourceArray, bool checkSumIsEnable, ILocalizationService localization)
    {
        // Сообщение с кодом ошибки - это сообщение с минимальным количеством байт (SlaveID, код функции, код ошибки)
        // Символ начала сообщения - 1 байт, сообщение с кодом ошибки ASCII-формате - 6 байт, символы конца сообщения 2 байта
        // Контрольная сумма LRC8 в ASCII-формате - 2 байта
        if (sourceArray.Length < 9 || (checkSumIsEnable && sourceArray.Length < 11))
            throw new Exception(localization.Get("Core.Modbus.InvalidMessageSize", ProtocolName, currentFunction.Number));
        
        if (!TryExtractFrame(sourceArray, out var asciiFrame))
            throw new Exception(localization.Get("Core.Modbus.InvalidMessageSize", ProtocolName, currentFunction.Number));

        var convertedArray = GetBytesArrayFromCharArray(asciiFrame);

        if (checkSumIsEnable && !ValidateCheckSum(convertedArray))
            throw new Exception(localization.Get("Core.Modbus.InvalidCheckSum", ProtocolName, currentFunction.Number));
        
        // SlaveID - 1 байт, LRC8 - 1 байт
        var pduArraySize = checkSumIsEnable ? convertedArray.Length - 2 : convertedArray.Length - 1;
        
        var pduArray = new byte[pduArraySize];
        
        Array.Copy(convertedArray, 1, pduArray, 0, pduArray.Length);
        
        return new ModbusResponse
        {
            SlaveID = convertedArray[0],
            PDU = DecodingPduResponse(currentFunction.Number, pduArray, localization)
        };
    }

    private static bool TryExtractFrame(byte[] sourceArray, out byte[] frame)
    {
        frame = Array.Empty<byte>();
        
        if (!CheckStartAndEndMessage(sourceArray))
            return false;
        
        var startIndex = Array.IndexOf(sourceArray, StartByte);
        
        for (var i = startIndex + 1; i < sourceArray.Length - 1; i++)
        {
            if (sourceArray[i] == EndByteCR && sourceArray[i + 1] == EndByteLF)
            {
                var frameLength = i + 2 - startIndex;
                frame = new byte[frameLength];
                
                Array.Copy(sourceArray, startIndex, frame, 0, frameLength);
                
                return true;
            }
        }

        return false;
    }
    
    private static bool CheckStartAndEndMessage(byte[] message)
    {
        return message.Contains(StartByte) &&
               message.Contains(EndByteCR) &&
               message.Contains(EndByteLF);
    }
    
    private static bool ValidateCheckSum(byte[] message)
    {
        if (message.Length < 2)
            return false;
        
        // Убираем последний байт с LRC8
        var mainPart = new byte[message.Length - 1];
        
        Array.Copy(message, mainPart, mainPart.Length);
        
        var actualLRC8 = CheckSum.Calculate_LRC8(mainPart);
        var expectedLRC8 = message[^1];
        
        return actualLRC8 == expectedLRC8;
    }
    
    private static byte[] GetBytesArrayFromCharArray(byte[] sourceArray)
    {
        // Отрезаем символы начала и конца сообщения
        var mainPart = new byte[sourceArray.Length - 3];

        Array.Copy(sourceArray, 1, mainPart, 0, mainPart.Length);

        return ConvertArrayToBytes(mainPart);
    }
    
    public static byte[] ConvertArrayToASCII(byte[] arrayBytes)
    {
        // В Modbus ASCII один байт представлен двумя ASCII символами
        var ASCII_Array = new char[arrayBytes.Length * 2];

        for (var i = 0; i < arrayBytes.Length; i++)
        {
            var element = arrayBytes[i].ToString("X2");

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