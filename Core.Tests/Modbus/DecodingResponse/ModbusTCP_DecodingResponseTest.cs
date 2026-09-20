using System.Buffers.Binary;
using Core.Models.Modbus.DataTypes;
using Core.Models.Modbus.Message;
using Core.Tests.Infrastructure;

namespace Core.Tests.Modbus.DecodingResponse;

public class ModbusTCP_DecodingResponseTest
{
    private readonly ModbusMessage _modbusMessage = new ModbusTCP_Message();
    private readonly ILocalizationService _localization = new TestLocalizationService();

    [Fact]
    public void WriteSingleFunction_Success()
    {
        const byte slaveId = 1;

        var selectedFunction = Function.PresetSingleRegister;

        const ushort address = 1;
        const ushort data = 0x10FF;

        var addressBytes = new byte[2];
        var dataBytes = new byte[2];

        BinaryPrimitives.WriteUInt16BigEndian(addressBytes, address);
        BinaryPrimitives.WriteUInt16BigEndian(dataBytes, data);
        
        var pdu = new byte[] { selectedFunction.Number }
            .Concat(addressBytes)
            .Concat(dataBytes)
            .ToArray();

        var result = _modbusMessage.DecodingResponse(
            selectedFunction,
            CreateTcpMessage(slaveId, pdu),
            false,
            _localization);

        Assert.Equal(0, result.ProtocolID);
        Assert.Equal(slaveId, result.SlaveID);
        
        Assert.IsType<PduResponseWriteSingle>(result.PDU);
        
        var resultPdu = result.PDU as PduResponseWriteSingle;
        
        Assert.NotNull(resultPdu);
        Assert.Equal(selectedFunction.Number, resultPdu.FunctionNumber);
        Assert.Equal(address, resultPdu.Address);
        Assert.Equal(data, resultPdu.Data);
    }
    
    [Fact]
    public void WriteMultipleFunction_Success()
    {
        const byte slaveId = 10;

        var selectedFunction = Function.PresetMultipleRegisters;

        const ushort address = 43;
        const ushort registerCount = 7;

        var addressBytes = new byte[2];
        var registerCountBytes = new byte[2];

        BinaryPrimitives.WriteUInt16BigEndian(addressBytes, address);
        BinaryPrimitives.WriteUInt16BigEndian(registerCountBytes, registerCount);
        
        var pdu = new byte[] { selectedFunction.Number }
            .Concat(addressBytes)
            .Concat(registerCountBytes)
            .ToArray();

        var result = _modbusMessage.DecodingResponse(
            selectedFunction,
            CreateTcpMessage(slaveId, pdu),
            false,
            _localization);

        Assert.Equal(0, result.ProtocolID);
        Assert.Equal(slaveId, result.SlaveID);
        
        Assert.IsType<PduResponseWriteMultiple>(result.PDU);
        
        var resultPdu = result.PDU as PduResponseWriteMultiple;
        
        Assert.NotNull(resultPdu);
        Assert.Equal(selectedFunction.Number, resultPdu.FunctionNumber);
        Assert.Equal(address, resultPdu.Address);
        Assert.Equal(registerCount, resultPdu.RegisterCount);
    }

    [Fact]
    public void ReadFunction_ReadCoilStatus_Success()
    {
        const byte slaveId = 1;

        var selectedFunction = Function.ReadCoilStatus;

        var data = new byte[] { 0x00, 0xFF, 0xFF, 0x00 };

        var dataByteCount = (byte)data.Length;

        var pdu = new byte[] { selectedFunction.Number, dataByteCount }
            .Concat(data)
            .ToArray();

        var result = _modbusMessage.DecodingResponse(
            selectedFunction,
            CreateTcpMessage(slaveId, pdu),
            false,
            _localization);
        
        Assert.Equal(0, result.ProtocolID);
        Assert.Equal(slaveId, result.SlaveID);
        
        Assert.IsType<PduResponseRead>(result.PDU);
        
        var resultPdu = result.PDU as PduResponseRead;

        Assert.NotNull(resultPdu);
        Assert.Equal(selectedFunction.Number, resultPdu.FunctionNumber);
        Assert.Equal(dataByteCount, resultPdu.Data.Length);
        Assert.Equal(data, resultPdu.Data);
    }

    [Fact]
    public void ReadFunction_WrongDataLength_Throws()
    {
        const byte slaveId = 1;

        var selectedFunction = Function.ReadCoilStatus;

        const byte declaredByteCount = 4;

        var data = new byte[] { 0xFF };

        var pdu = new byte[] { selectedFunction.Number, declaredByteCount }
            .Concat(data)
            .ToArray();

        Assert.Throws<Exception>(() =>
            _modbusMessage.DecodingResponse(
                selectedFunction,
                CreateTcpMessage(slaveId, pdu),
                false,
                _localization));
    }

    [Fact]
    public void ReadFunction_ZeroDataLength_Throws()
    {
        const byte slaveId = 1;

        var selectedFunction = Function.ReadCoilStatus;

        const byte declaredByteCount = 0;

        var data = new byte[] { 0xFF, 0x00 };

        var pdu = new byte[] { selectedFunction.Number, declaredByteCount }
            .Concat(data)
            .ToArray();

        Assert.Throws<Exception>(() =>
            _modbusMessage.DecodingResponse(
                selectedFunction,
                CreateTcpMessage(slaveId, pdu),
                false,
                _localization));
    }

    [Fact]
    public void AnyFunction_ErrorCode_Throws()
    {
        const byte slaveId = 1;

        var selectedFunction = Function.ReadCoilStatus;
        var selectedFunctionErrorNumber = selectedFunction.Number + 0x80;

        // Код ошибки должен быть в диапазоне от 1 до 11 включительно
        for (byte errorCode = 1; errorCode <= 11; errorCode++)
        {
            // Этот код не используется
            if (errorCode == 9)
                continue;

            var pdu = new byte[] { (byte)selectedFunctionErrorNumber, errorCode };

            var exception = Record.Exception(() =>
                _modbusMessage.DecodingResponse(
                    selectedFunction,
                    CreateTcpMessage(slaveId, pdu),
                    false,
                    _localization));

            Assert.IsType<ModbusException>(exception);
        }
    }

    [Fact]
    public void ShortResponse_PresetSingleRegister_Throws()
    {
        const byte slaveId = 3;

        var selectedFunction = Function.PresetSingleRegister;

        var address = new byte[] { 0x00, 0x01 };

        var data = new byte[] { 0x10 };

        var pdu = new byte[] { selectedFunction.Number }
            .Concat(address)
            .Concat(data)
            .ToArray();

        Assert.Throws<Exception>(() =>
            _modbusMessage.DecodingResponse(
                selectedFunction,
                CreateTcpMessage(slaveId, pdu),
                false,
                _localization));
    }

    [Fact]
    public void OddDataByteCount_ReadHoldingRegisters_Throws()
    {
        const byte slaveId = 7;

        var selectedFunction = Function.ReadHoldingRegisters;

        var data = new byte[] { 0x10 };

        var lengthOfData = (byte)data.Length;

        var pdu = new byte[] { selectedFunction.Number, lengthOfData }
            .Concat(data)
            .ToArray();

        Assert.Throws<Exception>(() =>
            _modbusMessage.DecodingResponse(
                selectedFunction,
                CreateTcpMessage(slaveId, pdu),
                false,
                _localization));
    }
    
    [Fact]
    public void TCPSpecific_WrongLengthOfPDU_Throws()
    {
        const byte slaveId = 16;

        var selectedFunction = Function.ReadCoilStatus;

        var message = new byte[11];

        // Operation number
        message[0] = 0;
        message[1] = 0;
        // Protocol ID
        message[2] = 0;
        message[3] = 0;
        // PDU length (делаем больше, чем нужно)
        ushort length = 7;
        message[4] = (byte)(length >> 8);
        message[5] = (byte)length;
        // PDU начинается тут
        message[6] = slaveId;
        message[7] = selectedFunction.Number;
        // Количество байт данных
        message[8] = 2;
        // Данные
        message[9] = 0x00;
        message[10] = 0xFF;

        Assert.Throws<Exception>(() =>
            _modbusMessage.DecodingResponse(selectedFunction, message, false, _localization));
    }

    private static byte[] CreateTcpMessage(byte slaveId, byte[] pdu)
    {
        var message = new byte[7 + pdu.Length];

        // Operation number
        message[0] = 0;
        message[1] = 0;
        // Protocol ID
        message[2] = 0;
        message[3] = 0;
        // PDU length
        var length = (ushort)(1 + pdu.Length);
        message[4] = (byte)(length >> 8);
        message[5] = (byte)length;
        
        message[6] = slaveId;

        Array.Copy(pdu, 0, message, 7, pdu.Length);

        return message;
    }
}
