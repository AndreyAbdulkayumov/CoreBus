using Core.Models.Modbus.DataTypes;

namespace Core.Models.Modbus.Message;

public class ModbusRTU_Message : ModbusMessage
{
    public override string ProtocolName { get; } = "Modbus RTU";

    public override byte[] CreateRequest(ModbusFunction function, MessageData data, ILocalizationService localization)
    {
        var PDU = Modbus_PDU.Create(function, data, localization);

        var TX = data.CheckSum_IsEnable 
            ? new byte[3 + PDU.Length] 
            : new byte[1 + PDU.Length];

        // Slave ID
        TX[0] = data.SlaveID;

        Array.Copy(PDU, 0, TX, 1, PDU.Length);

        // CRC16
        if (data.CheckSum_IsEnable)
        {
            var CRC16 = CheckSum.Calculate_CRC16(TX, data.Polynom);
            
            TX[TX.Length - 2] = CRC16[0];  // Предпоследний элемент
            TX[TX.Length - 1] = CRC16[1];  // Последний элемент
        }

        return TX;
    }

    public override ModbusResponse DecodingResponse(ModbusFunction currentFunction, byte[] sourceArray, bool checkSumIsEnable, ILocalizationService localization)
    {
        if (!CheckMinimalSize(currentFunction, sourceArray, checkSumIsEnable, localization))
            throw new Exception(localization.Get("Core.Modbus.InvalidMessageSize", ProtocolName, currentFunction.Number));
        
        if (checkSumIsEnable && !ValidateCheckSum(sourceArray))
            throw new Exception(localization.Get("Core.Modbus.InvalidCheckSum", ProtocolName, currentFunction.Number));
        
        var decodingResponse = new ModbusResponse
        {
            SlaveID = sourceArray[0],
            Command = sourceArray[1]
        };

        CheckErrorCode(TypeOfModbus.RTU, ref decodingResponse, sourceArray, localization);

        if (currentFunction is ModbusReadFunction)
        {
            const int lengthOfDataByteIndex = 2;
            
            decodingResponse.LengthOfData = sourceArray[lengthOfDataByteIndex];

            if (decodingResponse.LengthOfData == 0)
            {
                throw new Exception(localization.Get("Core.Modbus.EmptyDataPartRtu", currentFunction.Number));
            }

            decodingResponse.Data = new byte[decodingResponse.LengthOfData];

            // Согласно документации на протокол Modbus:
            // В ответном пакете Modbus RTU на команды чтения информационная часть начинается с четвертого байта.
            // Байт с количеством байт данных - третий.
            if (!CheckReadedDataLength(sourceArray, lengthOfDataByteIndex, checkSumIsEnable))
                throw new Exception(localization.Get("Core.Modbus.InvalidDataLength", ProtocolName, currentFunction.Number));
                
            Array.Copy(sourceArray, 3, decodingResponse.Data, 0, decodingResponse.LengthOfData);

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

    private static bool CheckMinimalSize(ModbusFunction function, byte[] data, bool checkSumIsEnable, ILocalizationService localization)
    {
        var crcSize = checkSumIsEnable ? 2 : 0;

        if (data.Length >= 2 && data[1] >= 0x80)
        {
            // SlaveID + Function number + Exception code
            return data.Length >= 3 + crcSize;
        }

        if (function.Number == Function.ReadCoilStatus.Number ||
            function.Number == Function.ReadDiscreteInputs.Number)
        {
            // SlaveID + Function number + Byte count + Data(1)
            return data.Length >= 4 + crcSize;
        }

        if (function.Number == Function.ReadHoldingRegisters.Number ||
            function.Number == Function.ReadInputRegisters.Number)
        {
            // SlaveID + Function number + Byte count + Data(2)
            return data.Length >= 5 + crcSize;
        }

        if (function is ModbusWriteFunction)
        {
            // Echo запроса: SlaveID + Function number + Address(2) + Value/Quantity(2)
            return data.Length >= 6 + crcSize;
        }

        throw new Exception(localization.Get("Core.Modbus.UnsupportedCommandCode", function.Number));
    }

    private static bool ValidateCheckSum(byte[] message)
    {
        if (message.Length < 2)
            return false;
        
        var calculatedCheckSum = CheckSum.Calculate_CRC16(message);

        var checkSumFromMessage = new byte[2];
        Array.Copy(message, message.Length - 2, checkSumFromMessage, 0, 2);
        
        return checkSumFromMessage.SequenceEqual(calculatedCheckSum);
    }
}
