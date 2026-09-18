using Core.Models.Modbus.DataTypes;

namespace Core.Models.Modbus.Message;

public class ModbusTCP_Message : ModbusMessage
{
    public override string ProtocolName { get; } = "Modbus TCP";

    public override byte[] CreateRequest(ModbusFunction function, MessageData data, ILocalizationService localization)
    {
        var PDU = Modbus_PDU.Create(function, data, localization);

        var TX = new byte[7 + PDU.Length];

        var packageNumberArray = BitConverter.GetBytes(PackageNumber);

        // 1 байт SlaveID + байты PDU
        var SlaveID_PDU_Length_Array = BitConverter.GetBytes((UInt16)(1 + PDU.Length));

        PackageNumber++;

        // Номер транзакции
        TX[0] = packageNumberArray[1];
        TX[1] = packageNumberArray[0];
        // Modbus ID
        TX[2] = 0x00;
        TX[3] = 0x00;
        // Количество байт далее (SlaveID + PDU)
        TX[4] = SlaveID_PDU_Length_Array[1];
        TX[5] = SlaveID_PDU_Length_Array[0];
        // Slave ID
        TX[6] = data.SlaveID;

        Array.Copy(PDU, 0, TX, 7, PDU.Length);

        return TX;
    }

    public override ModbusResponse DecodingResponse(ModbusFunction currentFunction, byte[] sourceArray, bool checkSumIsEnable, ILocalizationService localization)
    {
        if (!CheckMinimalSize(currentFunction, sourceArray, localization))
            throw new Exception(localization.Get("Core.Modbus.InvalidMessageSize", ProtocolName, currentFunction.Number));
        
        var decodingResponse = new ModbusResponse();

        var temp = new byte[2];

        temp[0] = sourceArray[1];
        temp[1] = sourceArray[0];
        decodingResponse.OperationNumber = (UInt16)BitConverter.ToInt16(temp, 0);
        temp[0] = sourceArray[3];
        temp[1] = sourceArray[2];
        decodingResponse.ProtocolID = (UInt16)BitConverter.ToInt16(temp, 0);
        temp[0] = sourceArray[5];
        temp[1] = sourceArray[4];
        decodingResponse.LengthOfPDU = (UInt16)BitConverter.ToInt16(temp, 0);
        
        if (!CheckPDULength(decodingResponse.LengthOfPDU, sourceArray))
            throw new Exception(localization.Get("Core.Modbus.InvalidMessageSize", ProtocolName, currentFunction.Number));
        
        decodingResponse.SlaveID = sourceArray[6];
        decodingResponse.Command = sourceArray[7];

        CheckErrorCode(TypeOfModbus.TCP, ref decodingResponse, sourceArray, localization);

        if (currentFunction is ModbusReadFunction)
        {
            const int lengthOfDataByteIndex = 8;
            
            decodingResponse.LengthOfData = sourceArray[lengthOfDataByteIndex];

            if (decodingResponse.LengthOfData == 0)
            {
                throw new Exception(localization.Get("Core.Modbus.EmptyDataPartTcp", currentFunction.Number));
            }

            decodingResponse.Data = new byte[decodingResponse.LengthOfData];

            // Согласно документации на протокол Modbus:
            // В ответном пакете Modbus TCP на команды чтения
            // байт с количеством данных — индекс 8, данные начинаются с индекса 9.
            if (!CheckReadedDataLength(sourceArray, lengthOfDataByteIndex, false))
                throw new Exception(localization.Get("Core.Modbus.InvalidDataLength", ProtocolName, currentFunction.Number));
            
            Array.Copy(sourceArray, 9, decodingResponse.Data, 0, decodingResponse.LengthOfData);

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
    
    private static bool CheckMinimalSize(ModbusFunction function, byte[] data, ILocalizationService localization)
    {
        // MBAP (7 байт, включая Unit ID) + PDU
        if (data.Length >= 8 && data[7] >= 0x80)
        {
            // MBAP + Function number + Exception code
            return data.Length >= 9;
        }

        if (function.Number == Function.ReadCoilStatus.Number ||
            function.Number == Function.ReadDiscreteInputs.Number)
        {
            // MBAP + Function number + Byte count + Data(1)
            return data.Length >= 10;
        }

        if (function.Number == Function.ReadHoldingRegisters.Number ||
            function.Number == Function.ReadInputRegisters.Number)
        {
            // MBAP + Function number + Byte count + Data(2)
            return data.Length >= 11;
        }

        if (function is ModbusWriteFunction)
        {
            // Echo запроса: MBAP + Function number + Address(2) + Value/Quantity(2)
            return data.Length >= 12;
        }

        throw new Exception(localization.Get("Core.Modbus.UnsupportedCommandCode", function.Number));
    }

    private static bool CheckPDULength(ushort expectedPDULength, byte[] data)
    {
        var actualPDULength = data.Length - 6; // 6 специфичных для этого протокола байт до SlaveID
        
        return expectedPDULength == actualPDULength;
    }
}
