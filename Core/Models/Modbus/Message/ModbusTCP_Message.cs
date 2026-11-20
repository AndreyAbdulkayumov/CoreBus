using System.Buffers.Binary;
using Core.Models.Modbus.DataTypes;

namespace Core.Models.Modbus.Message;

public class ModbusTCP_Message : ModbusMessage
{
    public override string ProtocolName { get; } = "Modbus TCP";

    /// <summary>
    /// Идентификатор протокола Modbus TCP согласно официальной документации
    /// </summary>
    private const ushort ProtocolIdentifier = 0;
    
    private ushort _transactionIdentifier;

    public override byte[] CreateRequest(ModbusFunction function, ModbusRequestData data, ILocalizationService localization)
    {
        var PDU = Modbus_PDU.Create(function, data, localization);

        var TX = new byte[7 + PDU.Length];

        var transactionIdentifierArray = BitConverter.GetBytes(_transactionIdentifier);
        var protocolIndetifierArray = BitConverter.GetBytes(ProtocolIdentifier);
        // 1 байт SlaveID + байты PDU
        var SlaveID_PDU_Length_Array = BitConverter.GetBytes((UInt16)(1 + PDU.Length));

        // Номер транзакции
        TX[0] = transactionIdentifierArray[1];
        TX[1] = transactionIdentifierArray[0];
        // Идентификатор протокола Modbus TCP
        TX[2] = protocolIndetifierArray[1];
        TX[3] = protocolIndetifierArray[0];
        // Количество байт далее (SlaveID + PDU)
        TX[4] = SlaveID_PDU_Length_Array[1];
        TX[5] = SlaveID_PDU_Length_Array[0];
        // Slave ID
        TX[6] = data.SlaveID;

        Array.Copy(PDU, 0, TX, 7, PDU.Length);

        _transactionIdentifier++;
        
        return TX;
    }

    public override ModbusResponse DecodingResponse(ModbusFunction currentFunction, RawResponse rawResponse, ILocalizationService localization)
    {
        const int slaveIdIndex = 6;
        
        var responseBytes = rawResponse.ResponseBytes;
        
        // Сообщение с кодом ошибки - это сообщение с минимальным количеством байт (SlaveID, код функции, код ошибки)
        // Сервисные поля - 6 байт, сообщение с кодом ошибки - 3 байта,
        if (responseBytes.Length < 9) 
            throw new Exception(localization.Get("Core.Modbus.InvalidMessageSize", ProtocolName, currentFunction.Number));

        var protocolIdentifier = BinaryPrimitives.ReadUInt16BigEndian(responseBytes.AsSpan(2, 2));
        
        if (protocolIdentifier != ProtocolIdentifier)
            throw new Exception(localization.Get("Core.Modbus.InvalidProtocolIdentifier", ProtocolName, ProtocolIdentifier));
        
        var length = BinaryPrimitives.ReadUInt16BigEndian(responseBytes.AsSpan(4, 2));
        
        if (!CheckLength(length, responseBytes))
            throw new Exception(localization.Get("Core.Modbus.InvalidMessageSize", ProtocolName, currentFunction.Number));
        
        var actualSlaveId = ValidateAndGetSlaveId(slaveIdIndex, rawResponse, localization);
        
        var pduArray = new byte[responseBytes.Length - 7];
        
        Array.Copy(responseBytes, 7, pduArray, 0, pduArray.Length);
        
        return new ModbusResponse()
        {
            SlaveID = actualSlaveId,
            PDU = DecodingPduResponse(currentFunction.Number, pduArray, localization)
        };
    }

    private static bool CheckLength(ushort expectedLength, byte[] data)
    {
        var actualLength = data.Length - 6; // 6 специфичных для этого протокола байт до SlaveID
        
        return expectedLength == actualLength;
    }
}
