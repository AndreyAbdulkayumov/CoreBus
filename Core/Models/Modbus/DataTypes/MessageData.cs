﻿namespace Core.Models.Modbus.DataTypes
{
    public abstract class MessageData
    {
        public byte SlaveID;
        public ushort Address;
        public bool CheckSum_IsEnable;
        public ushort Polynom;
        public int NumberOfRegisters;
    }

    public class ReadTypeMessage : MessageData
    {
        public ReadTypeMessage(byte slaveID, ushort address, int numberOfRegisters, bool checkSum_IsEnable, ushort polynom = 0xA001)
        {
            SlaveID = slaveID;
            Address = address;
            NumberOfRegisters = numberOfRegisters;
            CheckSum_IsEnable = checkSum_IsEnable;
            Polynom = polynom;
        }
    }

    public class WriteTypeMessage : MessageData
    {
        public byte[] WriteData;

        public WriteTypeMessage(byte slaveID, ushort address, byte[] writeData, int numberOfRegisters, bool checkSum_IsEnable, ushort polynom = 0xA001)
        {
            SlaveID = slaveID;
            Address = address;
            WriteData = writeData;
            NumberOfRegisters = numberOfRegisters;
            CheckSum_IsEnable = checkSum_IsEnable;
            Polynom = polynom;
        }
    }
}
