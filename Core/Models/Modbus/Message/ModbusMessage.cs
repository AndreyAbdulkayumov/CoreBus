using System.Buffers.Binary;
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

    protected PduResponse DecodingPduResponse(byte[] pduArray, ILocalizationService localization)
    {
        if (pduArray.Length < 2)
            throw new Exception(localization.Get("Core.Modbus.InvalidMessageSizeSimple", ProtocolName));
        
        CheckErrorCode(pduArray, localization);

        var functionNumber = pduArray[0];
        
        if (Function.AllReadFunctionNumbers.Contains(functionNumber))
        {
            return CreatePduResponseRead(pduArray, localization);
        }

        if (Function.AllWriteFunctionNumbers.Contains(functionNumber))
        {
            return CreatePduResponseWrite(pduArray, localization);
        }

        throw new Exception(localization.Get("Core.Modbus.UnsupportedCommandCode", functionNumber));
    }

    private PduResponseRead CreatePduResponseRead(byte[] pduArray, ILocalizationService localization)
    {
        var functionNumber = pduArray[0];
        
        // Согласно документации на протокол Modbus:
        // В PDU ответного пакета на команды чтения информационная часть начинается с третьего байта.
        // Байт с количеством байт данных - второй.
        var dataBytesCount = pduArray[1];
            
        if (dataBytesCount == 0 || dataBytesCount != pduArray.Length - 2)
            throw new Exception(localization.Get("Core.Modbus.InvalidDataLength", ProtocolName, functionNumber));
            
        var data = new byte[dataBytesCount];
            
        Array.Copy(pduArray, 2, data, 0,  dataBytesCount);

        // Реверс байтов нужен только функциям, работающим с регистрами (номера 3 и 4).
        if (functionNumber == Function.ReadHoldingRegisters.Number ||
            functionNumber == Function.ReadInputRegisters.Number)
        {
            if (data.Length % 2 != 0)
                throw new Exception(localization.Get("Core.Modbus.InvalidDataLength", ProtocolName, functionNumber));
            
            return new PduResponseRead(functionNumber, ReverseLowAndHighBytesInWords(data));
        }

        return new PduResponseRead(functionNumber, data);
    }

    private PduResponse CreatePduResponseWrite(byte[] pduArray, ILocalizationService localization)
    {
        var functionNumber = pduArray[0];
        
        if (pduArray.Length < 5)
            throw new Exception(localization.Get("Core.Modbus.InvalidMessageSize", ProtocolName, functionNumber));
        
        var address = BinaryPrimitives.ReadUInt16BigEndian(pduArray.AsSpan(1, 2));
        
        if (functionNumber == Function.ForceSingleCoil.Number ||
            functionNumber == Function.PresetSingleRegister.Number)
        { 
            var data = BinaryPrimitives.ReadUInt16BigEndian(pduArray.AsSpan(3, 2));
            return new PduResponseWriteSingle(functionNumber, address, data);
        }
        
        var registerCount = BinaryPrimitives.ReadUInt16BigEndian(pduArray.AsSpan(3, 2));
        return new PduResponseWriteMultiple(functionNumber, address, registerCount);
    }

    private void CheckErrorCode(byte[] pduArray, ILocalizationService localization)
    {
        // Согласно документации на протокол Modbus:
        // Если значение в поле команды больше 0x80, то это ошибка.
        // Значение команды = значение в поле команды - 0x80
        var command = pduArray[0];
        
        if (command > 0x80)
        {
            var functionCode = (byte)(command - 0x80);
            var errorCode = pduArray[1];
            
            GetModbusException(functionCode, errorCode, localization);
        }
    }

    private static byte[] ReverseLowAndHighBytesInWords(byte[] sourceArray)
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
    
    private static void GetModbusException(byte functionCode, byte errorCode, ILocalizationService localization)
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
