using Core.Models.Modbus.DataTypes;

namespace Core.Models.Modbus.Message;

public class ModbusRTU_Message : ModbusMessage
{
    public override string ProtocolName { get; } = "Modbus RTU";

    public override byte[] CreateRequest(ModbusFunction function, ModbusRequestData data, ILocalizationService localization)
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
            var CRC16 = CheckSum.Calculate_CRC16(TX);
            
            TX[^2] = CRC16[0];  // Предпоследний элемент
            TX[^1] = CRC16[1];  // Последний элемент
        }

        return TX;
    }

    public override ModbusResponse DecodingResponse(ModbusFunction currentFunction, RawResponse rawResponse, ILocalizationService localization)
    {
        const int slaveIdIndex = 0;
        
        var responseBytes = rawResponse.ResponseBytes;
        var checkSumIsEnable = rawResponse.CheckSumIsEnable;
        
        // Сообщение с кодом ошибки - это сообщение с минимальным количеством байт (SlaveID, код функции, код ошибки)
        // Сообщение с кодом ошибки - 3 байта
        // Контрольная сумма CRC16 - 2 байта
        if (responseBytes.Length < 3 || (checkSumIsEnable && responseBytes.Length < 5))
            throw new Exception(localization.Get("Core.Modbus.InvalidMessageSize", ProtocolName, currentFunction.Number));
        
        if (checkSumIsEnable && !ValidateCheckSum(responseBytes))
            throw new Exception(localization.Get("Core.Modbus.InvalidCheckSum", ProtocolName, currentFunction.Number));
        
        var actualSlaveId = ValidateAndGetSlaveId(slaveIdIndex, rawResponse, localization);
        
        // SlaveID - 1 байт, CRC16 - 2 байта
        var pduArraySize = checkSumIsEnable ? responseBytes.Length - 3 : responseBytes.Length - 1;
        
        var pduArray = new byte[pduArraySize];
        
        Array.Copy(responseBytes, 1, pduArray, 0, pduArray.Length);
        
        return new ModbusResponse
        {
            SlaveID = actualSlaveId,
            PDU = DecodingPduResponse(currentFunction.Number, pduArray, localization)
        };
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
