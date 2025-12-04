using ReactiveUI;
using DynamicData;
using System.Collections.ObjectModel;
using System.Reactive;
using Core.Models.Settings.DataTypes;
using ViewModels.ModbusClient.Manual.DataTypes;
using ViewModels.ModbusClient.Manual.WriteFields.DataItems;
using ViewModels.ModbusClient.Manual.WriteFields.DataTypes;

namespace ViewModels.ModbusClient.Manual.WriteFields;

public class MultipleCoils_VM : ReactiveObject, IWriteField_VM
{
    private ObservableCollection<MultipleCoils_Item> _items = new ObservableCollection<MultipleCoils_Item>();

    public ObservableCollection<MultipleCoils_Item> Items
    {
        get => _items;
        set => this.RaiseAndSetIfChanged(ref _items, value);
    }

    // У этого элемента нет полей ввода, поэтому он не может иметь ошибок валидации
    public bool HasValidationErrors => false;
    public string? ValidationMessage => null;

    public ReactiveCommand<Unit, Unit> Command_AddRegister { get; }


    public MultipleCoils_VM()
    {
        Command_AddRegister = ReactiveCommand.Create(() =>
        {
            AddCoilItem(null);
        });
    }

    private void AddCoilItem(bool? value)
    {
        Items.Add(new MultipleCoils_Item(
            startAddressAddition: Items.Count,
            removeItemHandler: RemoveWriteDataItem,
            isLogicOne: value
            ));
    }

    public WriteData GetData()
    {
        return PrepareData();
    }

    private WriteData PrepareData()
    {
        if (Items.Count == 0)
        {
            return new WriteData(Array.Empty<byte>(), 0);
        }

        int[] bitArray = Items.Select(e => e.Logic_One ? 1 : 0).ToArray();

        List<byte> result = new List<byte>();

        byte temp = 0;

        for (int i = 0; i < bitArray.Length; i++)
        {
            temp |= (byte)(bitArray[i] << (i % 8));

            if ((i + 1) % 8 == 0)
            {
                result.Add(temp);
                temp = 0;
            }
        }

        if (result.Count == 0 || temp != 0)
        {
            result.Add(temp);
        }

        return new WriteData(result.ToArray(), bitArray.Length);
    }

    public void SetDataFromMacros(ModbusMacrosWriteInfo data)
    {
        Items.Clear();

        if (data.WriteBuffer == null || data.WriteBuffer.Length == 0)
        {
            return;
        }

        int totalBits = data.NumberOfWriteRegisters;

        for (int i = 0; i < data.WriteBuffer.Length; i++)
        {
            byte currentByte = data.WriteBuffer[i];

            for (int bitIndex = 0; bitIndex < 8; bitIndex++)
            {
                // Проверяем, не превышает ли индекс общего количества битов
                if (i * 8 + bitIndex >= totalBits)
                    break;

                bool logicOne = (currentByte & (1 << bitIndex)) != 0;

                AddCoilItem(logicOne);
            }
        }
    }

    public ModbusMacrosWriteInfo GetMacrosData()
    {
        WriteData data = PrepareData();

        return new ModbusMacrosWriteInfo()
        {
            WriteBuffer = data.Data,
            NumberOfWriteRegisters = data.NumberOfRegisters,
        };
    }

    private void RemoveWriteDataItem(Guid selectedId)
    {
        int AddressCounter = 0;

        var newCollection = Items
            .Where(e => e.Id != selectedId)
            .ToList();

        newCollection.ForEach(e =>
        {
            e.StartAddressAddition = "+" + AddressCounter.ToString();

            AddressCounter++;
        });

        Items.Clear();
        Items.AddRange(newCollection);
    }
}
