using Core.Models.Modbus.DataTypes;

namespace Core.Models.Modbus.Message;

public abstract class ModbusMessage
{
    /***********************************************/

    // Должны быть определены в наследниках:
    // реализациях протокола Modbus RTU, ASCII, TCP

    public abstract string ProtocolName { get; }

    public abstract byte[] CreateRequest(ModbusFunction function, MessageData data, ILocalizationService localization);
    public abstract ModbusResponse DecodingResponse(ModbusFunction function, byte[] sourceArray, bool checkSumIsEnable, ILocalizationService localization);

    /***********************************************/

    protected ulong PackageNumber = 0;

    protected enum TypeOfModbus
    {
        TCP,
        RTU,
        ASCII
    }

    protected void CheckErrorCode(TypeOfModbus modbusType, ref ModbusResponse decoding, byte[] massive, ILocalizationService localization)
    {
        // Согласно документации на протокол Modbus:
        // Если значение в поле команды больше 0x80, то это ошибка.
        // Значение команды = значение в поле команды - 0x80

        if (decoding.Command > 0x80)
        {
            var functionCode = decoding.Command - 0x80;

            decoding.Data = new byte[1]; // Код ошибки занимает 1 байт

            // Modbus TCP
            // [0],[1] - Package ID, [2],[3] - Modbus ID, [4],[5] - Length of PDU
            // [6] - Slave ID, [7] - Command, [8] - Error code
            if (modbusType == TypeOfModbus.TCP)
            {
                decoding.Data[0] = massive[8];
            }

            // Modbus RTU / ASCII 
            // [0] - Slave ID, [1] - Command, [2] - Error code,
            // [3] - CheckSum_low, [4] - CheckSum_high
            else
            {
                decoding.Data[0] = massive[2];
            }

            GetModbusException(decoding.Data[0], (byte)functionCode, localization);
        }
    }

    protected static byte[] ReverseLowAndHighBytesInWords(byte[] sourceArray)
    {
        if (sourceArray.Length < 2)
        {
            return sourceArray;
        }

        for (var i = 0; i < sourceArray.Length; i += 2)
        {
            var temp = sourceArray[i];
            sourceArray[i] = sourceArray[i + 1];
            sourceArray[i + 1] = temp;
        }

        return sourceArray;
    }

    protected static bool CheckReadedDataLength(byte[] message, int dataLengthIndex, bool checkSumIsEnable)
    {
        if (message.Length <= dataLengthIndex)
            return false;

        var expectedDataLength = message[dataLengthIndex];

        var serviceByteCount = dataLengthIndex + 1; // С учетом байта количества данных
        
        var actualDataLength = checkSumIsEnable 
            ? message.Length - serviceByteCount - 2 // CRC16 и LRC8 занимают по два байта
            : message.Length - serviceByteCount;
        
        return expectedDataLength == actualDataLength;
    }
    
    private static void GetModbusException(byte errorCode, byte functionCode, ILocalizationService localization)
    {
        switch (errorCode)
        {
            case 1:
                throw new ModbusException(functionCode, errorCode,
                    localization.Get("Core.Modbus.ExceptionCode1"));

            case 2:
                throw new ModbusException(functionCode, errorCode,
                    localization.Get("Core.Modbus.ExceptionCode2"));

            case 3:
                throw new ModbusException(functionCode, errorCode,
                    localization.Get("Core.Modbus.ExceptionCode3"));

            case 4:
                throw new ModbusException(functionCode, errorCode,
                    localization.Get("Core.Modbus.ExceptionCode4"));

            case 5:
                throw new ModbusException(functionCode, errorCode,
                    localization.Get("Core.Modbus.ExceptionCode5"));

            case 6:
                throw new ModbusException(functionCode, errorCode,
                    localization.Get("Core.Modbus.ExceptionCode6"));

            case 7:
                throw new ModbusException(functionCode, errorCode,
                    localization.Get("Core.Modbus.ExceptionCode7"));

            case 8:
                throw new ModbusException(functionCode, errorCode,
                    localization.Get("Core.Modbus.ExceptionCode8"));

            case 10:
                throw new ModbusException(functionCode, errorCode,
                    localization.Get("Core.Modbus.ExceptionCode10"));

            case 11:
                throw new ModbusException(functionCode, errorCode,
                    localization.Get("Core.Modbus.ExceptionCode11"));

            default:
                throw new Exception(localization.Get("Core.Modbus.UnknownErrorWithCodes", functionCode, errorCode));
        }
    }
}
