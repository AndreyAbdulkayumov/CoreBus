using Core.Models.Modbus.DataTypes;
using Services.Interfaces;

namespace Core.Models.Modbus.Message;

public static class Modbus_PDU
{
    public static byte[] Create(ModbusFunction function, MessageData data, ILocalizationService localization)
    {
        WriteTypeMessage? dataForWrite = data as WriteTypeMessage;
        ReadTypeMessage? dataForRead = data as ReadTypeMessage;

        if (dataForRead != null && function.Number >= 1 && function.Number <= 4)
        {
            return Create_Read(function, dataForRead);
        }

        else if (dataForWrite != null && (function.Number == 5 || function.Number == 6))
        {
            return Create_Write(function, dataForWrite);
        }

        else if (dataForWrite != null && function.Number == 15)
        {
            return Create_WriteMultipleCoils(function, dataForWrite);
        }

        else if (dataForWrite != null && function.Number == 16)
        {
            return Create_WriteMultipleRegisters(function, dataForWrite);
        }

        else
        {
            throw new Exception(localization.Get("Core.Modbus.PduBuildErrorUnsupportedFunction", function.Number));
        }
    }

    private static byte[] Create_Read(ModbusFunction readFunction, ReadTypeMessage data)
    {
        byte[] PDU = new byte[5];

        byte[] addressArray = BitConverter.GetBytes(data.Address);
        byte[] numberOfRegistersArray = BitConverter.GetBytes(data.NumberOfRegisters);

        // Function number
        PDU[0] = readFunction.Number;
        // Address 
        PDU[1] = addressArray[1];
        PDU[2] = addressArray[0];
        // Amount of readed registers
        PDU[3] = numberOfRegistersArray[1];
        PDU[4] = numberOfRegistersArray[0];

        return PDU;
    }

    private static byte[] Create_Write(ModbusFunction writeFunction, WriteTypeMessage data)
    {
        byte[] PDU = new byte[5];

        byte[] addressArray = BitConverter.GetBytes(data.Address);

        // Function number
        PDU[0] = writeFunction.Number;
        // Address 
        PDU[1] = addressArray[1];
        PDU[2] = addressArray[0];
        // Data
        PDU[3] = data.WriteData[1];
        PDU[4] = data.WriteData[0];

        return PDU;
    }

    private static byte[] Create_WriteMultipleCoils(ModbusFunction writeFunction, WriteTypeMessage data)
    {
        int offsetPDU = 6;
        byte[] PDU = new byte[offsetPDU + data.WriteData.Length];

        byte[] addressArray = BitConverter.GetBytes(data.Address);
        byte[] numberOfRegistersArray = BitConverter.GetBytes(data.NumberOfRegisters);

        // Function number
        PDU[0] = writeFunction.Number;
        // Address 
        PDU[1] = addressArray[1];
        PDU[2] = addressArray[0];
        // Amount of write registers
        PDU[3] = numberOfRegistersArray[1];
        PDU[4] = numberOfRegistersArray[0];
        // Amount of byte next
        PDU[5] = (byte)data.WriteData.Length;
        // Data
        for (int i = 0; i < data.WriteData.Length; i++)
        {
            PDU[i + offsetPDU] = data.WriteData[i];
        }

        return PDU;
    }

    private static byte[] Create_WriteMultipleRegisters(ModbusFunction writeFunction, WriteTypeMessage data)
    {
        int offsetPDU = 6;
        byte[] PDU = new byte[offsetPDU + data.WriteData.Length];

        byte[] addressArray = BitConverter.GetBytes(data.Address);
        byte[] numberOfRegistersArray = BitConverter.GetBytes(data.NumberOfRegisters);

        // Function number
        PDU[0] = writeFunction.Number;
        // Address 
        PDU[1] = addressArray[1];
        PDU[2] = addressArray[0];
        // Amount of write registers
        PDU[3] = numberOfRegistersArray[1];
        PDU[4] = numberOfRegistersArray[0];
        // Amount of byte next
        PDU[5] = (byte)data.WriteData.Length;

        byte temp;

        // Data
        for (int i = 0; i < data.WriteData.Length; i += 2)
        {
            temp = data.WriteData[i];
            PDU[i + offsetPDU] = data.WriteData[i + 1];
            PDU[i + offsetPDU + 1] = temp;
        }

        return PDU;
    }
}
