﻿using MessageBox_Core;
using ReactiveUI;
using System.Reactive;

namespace ViewModels.ModbusClient.DataTypes
{
    public class BinaryRepresentation_ItemData : ReactiveObject
    {
        public string Address { get; private set; }
        public BinaryDataItemGroup[] BinaryData { get; private set; }
        
        public ReactiveCommand<Unit, Unit> Command_Copy_BinaryWord { get; }

        public BinaryRepresentation_ItemData(string address, BinaryDataItemGroup[] binaryData,
            Action<string, MessageType> messageBox, Func<string, Task> copyToClipboard)
        {
            Address = address;
            BinaryData = binaryData;

            Command_Copy_BinaryWord = ReactiveCommand.CreateFromTask(async () =>
            {
                string Data = string.Empty;

                foreach (var group in BinaryData)
                {
                    Data += string.Concat(group.GroupData.Select(element => element.Bit));
                }

                await copyToClipboard(Data);
            });
            Command_Copy_BinaryWord.ThrownExceptions.Subscribe(error => messageBox.Invoke($"Ошибка копирования данных из регистра с адресом {Address} в буфер обмена.\n\n" + error.Message, MessageType.Error));
        }
    }

    public class BinaryDataItemGroup
    {
        public BinaryDataItem[] GroupData { get; private set; }

        public BinaryDataItemGroup(BinaryDataItem[] groupData)
        {
            GroupData = groupData;
        }
    }

    public class BinaryDataItem
    {
        public string Bit { get; set; } = "0";
        public bool IsChange { get; set; } = true;
    }
}
