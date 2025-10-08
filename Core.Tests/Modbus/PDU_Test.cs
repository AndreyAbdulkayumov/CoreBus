using Core.Models.Modbus.DataTypes;
using Core.Models.Modbus.Message;

namespace Core.Tests.Modbus;

public class PDU_Test
{
    [Fact]
    public void Test_ReadCoilStatus_PDU_Creation()
    {
        CheckReadFunction(
            SelectedFunction: Function.ReadCoilStatus,
            Address: 16,
            NumberOfRegisters: 2
            );
    }

    [Fact]
    public void Test_ReadDiscreteInputs_PDU_Creation()
    {
        CheckReadFunction(
            SelectedFunction: Function.ReadDiscreteInputs,
            Address: 2,
            NumberOfRegisters: 4
            );
    }

    [Fact]
    public void Test_ReadHoldingRegisters_PDU_Creation()
    {
        CheckReadFunction(
            SelectedFunction: Function.ReadHoldingRegisters,
            Address: 0,
            NumberOfRegisters: 3
            );
    }

    [Fact]
    public void Test_ReadHoldingRegisters_PDU_Creation_ZeroRegisters()
    {
        CheckReadFunction(
            SelectedFunction: Function.ReadHoldingRegisters,
            Address: 0,
            NumberOfRegisters: 0
            );
    }

    [Fact]
    public void Test_ReadHoldingRegisters_PDU_Creation_MaxAddressZeroRegisters()
    {
        CheckReadFunction(
            SelectedFunction: Function.ReadHoldingRegisters,
            Address: 0xFFFF,
            NumberOfRegisters: 0
            );
    }

    [Fact]
    public void Test_ReadInputRegisters_PDU_Creation()
    {
        CheckReadFunction(
            SelectedFunction: Function.ReadInputRegisters,
            Address: 45,
            NumberOfRegisters: 7
            );
    }

    [Fact]
    public void Test_ForceSingleCoil_PDU_Creation()
    {
        CheckSingleWriteFunction(
            SelectedFunction: Function.ForceSingleCoil,
            Address: 82,
            WriteData: 0xFF00
            );
    }

    [Fact]
    public void Test_PresetSingleRegister_PDU_Creation()
    {
        CheckSingleWriteFunction(
            SelectedFunction: Function.PresetSingleRegister,
            Address: 76,
            WriteData: 0x5942
            );
    }

    [Fact]
    public void Test_ForceMultipleCoils_PDU_Creation()
    {
        CheckMultiplyWriteCoilsFunction(
            address: 79,
            bitArray: new int[] { 1, 0, 0, 0, 1, 1, 0 }
            );
    }

    [Fact]
    public void Test_PresetMultipleRegisters_PDU_Creation()
    {
        CheckMultiplyWriteRegistersFunction(
            Address: 79,
            WriteData: new UInt16[] { 0x0F50, 0x4575, 0x0789, 0x0030 }
            );
    }


    // Вспомогательные функции

    private void CheckReadFunction(ModbusReadFunction SelectedFunction, UInt16 Address, UInt16 NumberOfRegisters)
    {
        MessageData Data = new ReadTypeMessage(
                16,
                Address,
                NumberOfRegisters,
                true
                );

        byte[] BytesArray_Actual = Modbus_PDU.Create(SelectedFunction, Data);

        byte[] AddressBytes = ModbusField.Get_Address(Address);
        byte[] NumberOfRegistersBytes = ModbusField.Get_NumberOfRegisters(NumberOfRegisters);

        byte[] BytesArray_Expected = new byte[]
        {
                SelectedFunction.Number,
                AddressBytes[1],
                AddressBytes[0],
                NumberOfRegistersBytes[1],
                NumberOfRegistersBytes[0]
        };

        Assert.Equal(BytesArray_Expected, BytesArray_Actual);
    }

    private void CheckSingleWriteFunction(ModbusWriteFunction SelectedFunction, UInt16 Address, UInt16 WriteData)
    {
        UInt16[] WriteDataArray = new UInt16[] { WriteData };

        byte[] bytes = BitConverter.GetBytes(WriteData);

        MessageData Data = new WriteTypeMessage(
            16,
            Address,
            bytes,
            1,
            true
            );

        byte[] BytesArray_Actual = Modbus_PDU.Create(SelectedFunction, Data);

        byte[] AddressBytes = ModbusField.Get_Address(Address);
        byte[] WriteDataBytes = ModbusField.Get_WriteData(WriteDataArray);

        if (WriteDataBytes.Length != 2)
        {
            throw new Exception("Длина записи для одного регистра должна быть 2 байта.");
        }

        byte[] BytesArray_Expected = new byte[]
        {
                SelectedFunction.Number,
                AddressBytes[1],
                AddressBytes[0],
                WriteDataBytes[0],
                WriteDataBytes[1]
        };

        Assert.Equal(BytesArray_Expected, BytesArray_Actual);
    }

    private void CheckMultiplyWriteCoilsFunction(UInt16 address, int[] bitArray)
    {
        ModbusWriteFunction selectedFunction = Function.ForceMultipleCoils;

        (byte[] writeBytes, int numberOfCoils) = ModbusField.Get_WriteDataFromMultipleCoils(bitArray);

        MessageData data = new WriteTypeMessage(
            16,
            address,
            writeBytes,
            numberOfCoils,
            true
            );

        byte[] bytesArray_Actual = Modbus_PDU.Create(selectedFunction, data);

        byte[] bytesArray_Expected = new byte[6 + writeBytes.Length];

        byte[] addressBytes = ModbusField.Get_Address(address);
        byte[] numberOfRegisters = ModbusField.Get_NumberOfRegisters((UInt16)numberOfCoils);

        bytesArray_Expected[0] = selectedFunction.Number;
        bytesArray_Expected[1] = addressBytes[1];
        bytesArray_Expected[2] = addressBytes[0];
        bytesArray_Expected[3] = numberOfRegisters[1];
        bytesArray_Expected[4] = numberOfRegisters[0];
        bytesArray_Expected[5] = (byte)writeBytes.Length;

        Array.Copy(writeBytes, 0, bytesArray_Expected, 6, writeBytes.Length);

        Assert.Equal(bytesArray_Expected, bytesArray_Actual);
    }

    private void CheckMultiplyWriteRegistersFunction(UInt16 Address, UInt16[] WriteData)
    {
        ModbusWriteFunction selectedFunction = Function.PresetMultipleRegisters;

        byte[] bytes = WriteData.SelectMany(BitConverter.GetBytes).ToArray();

        MessageData Data = new WriteTypeMessage(
            16,
            Address,
            bytes,
            WriteData.Length,
            true
            );

        byte[] BytesArray_Actual = Modbus_PDU.Create(selectedFunction, Data);

        byte[] AddressBytes = ModbusField.Get_Address(Address);
        byte[] NumberOfRegisters = ModbusField.Get_NumberOfRegisters((UInt16)WriteData.Length);
        byte[] WriteDataBytes = ModbusField.Get_WriteData(WriteData);

        if (WriteDataBytes.Length != WriteData.Length * 2)
        {
            throw new Exception("Некорректное количество байт в WriteDataBytes.");
        }

        byte[] BytesArray_Expected = new byte[6 + WriteDataBytes.Length];

        BytesArray_Expected[0] = selectedFunction.Number;
        BytesArray_Expected[1] = AddressBytes[1];
        BytesArray_Expected[2] = AddressBytes[0];
        BytesArray_Expected[3] = NumberOfRegisters[1];
        BytesArray_Expected[4] = NumberOfRegisters[0];
        BytesArray_Expected[5] = (byte)WriteDataBytes.Length;

        Array.Copy(WriteDataBytes, 0, BytesArray_Expected, 6, WriteDataBytes.Length);

        Assert.Equal(BytesArray_Expected, BytesArray_Actual);
    }
}
