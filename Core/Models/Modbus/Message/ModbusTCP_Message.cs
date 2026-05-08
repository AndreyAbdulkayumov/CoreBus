using Core.Models.Modbus.DataTypes;
using Services.Interfaces;

namespace Core.Models.Modbus.Message;

public class ModbusTCP_Message : ModbusMessage
{
    public override string ProtocolName { get; } = "Modbus TCP";

    public override byte[] CreateMessage(ModbusFunction function, MessageData data, ILocalizationService localization)
    {
        byte[] PDU = Modbus_PDU.Create(function, data, localization);

        byte[] TX;

        TX = new byte[7 + PDU.Length];

        byte[] packageNumberArray = BitConverter.GetBytes(PackageNumber);

        // 1 байт SlaveID + байты PDU
        byte[] SlaveID_PDU_Length_Array = BitConverter.GetBytes((UInt16)(1 + PDU.Length));

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

    public override ModbusResponse DecodingMessage(ModbusFunction currentFunction, byte[] sourceArray, ILocalizationService localization)
    {
        var decodingResponse = new ModbusResponse();

        byte[] temp = new byte[2];

        temp[0] = sourceArray[1];
        temp[1] = sourceArray[0];
        decodingResponse.OperationNumber = (UInt16)BitConverter.ToInt16(temp, 0);
        temp[0] = sourceArray[3];
        temp[1] = sourceArray[2];
        decodingResponse.ProtocolID = (UInt16)BitConverter.ToInt16(temp, 0);
        temp[0] = sourceArray[5];
        temp[1] = sourceArray[4];
        decodingResponse.LengthOfPDU = (UInt16)BitConverter.ToInt16(temp, 0);
        decodingResponse.SlaveID = sourceArray[6];
        decodingResponse.Command = sourceArray[7];

        CheckErrorCode(TypeOfModbus.TCP, ref decodingResponse, sourceArray, localization);

        if (currentFunction is ModbusReadFunction)
        {
            decodingResponse.LengthOfData = sourceArray[8];

            if (decodingResponse.LengthOfData == 0)
            {
                throw new Exception(localization.Get("Core.Modbus.EmptyDataPartTcp", currentFunction.Number));
            }

            decodingResponse.Data = new byte[decodingResponse.LengthOfData];

            // Согласно документации на протокол Modbus:
            // В ответном пакете Modbus TCP на команды чтения
            // информационная часть начинается с 9 байта.
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
}
