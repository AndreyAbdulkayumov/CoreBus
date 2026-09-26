using System.Buffers.Binary;
using Core.Models.Modbus;
using Core.Models.Modbus.DataTypes;
using Core.Models.Modbus.Message;
using Core.Tests.Infrastructure;

namespace Core.Tests.Modbus.DecodingResponse;

public class ModbusASCII_DecodingResponseTest
{
    private readonly ModbusMessage _modbusMessage = new ModbusASCII_Message();
    private readonly ILocalizationService _localization = new TestLocalizationService();
    
    [Fact]
    public void WriteSingleFunction_Success()
    {
        const byte slaveId = 1;
        
        var selectedFunction = Function.PresetSingleRegister;

        const ushort address = 13;
        const ushort data = 0x089F;
        
        const bool checkSumIsEnable = true;

        var addressBytes = new byte[2];
        var dataBytes = new byte[2];

        BinaryPrimitives.WriteUInt16BigEndian(addressBytes, address);
        BinaryPrimitives.WriteUInt16BigEndian(dataBytes, data);
        
        var message = new byte[] { slaveId, selectedFunction.Number }
            .Concat(addressBytes)
            .Concat(dataBytes)
            .ToArray();
        
        var result = _modbusMessage.DecodingResponse(
            selectedFunction, 
            CreateAsciiMessageFromBytes(message, checkSumIsEnable),
            checkSumIsEnable,
            _localization);
        
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

        const ushort address = 18;
        const ushort registerCount = 4;

        var addressBytes = new byte[2];
        var registerCountBytes = new byte[2];

        BinaryPrimitives.WriteUInt16BigEndian(addressBytes, address);
        BinaryPrimitives.WriteUInt16BigEndian(registerCountBytes, registerCount);
        
        var message = new byte[] { slaveId, selectedFunction.Number }
            .Concat(addressBytes)
            .Concat(registerCountBytes)
            .ToArray();

        var result = _modbusMessage.DecodingResponse(
            selectedFunction, 
            CreateAsciiMessageFromBytes(message, true),
            true,
            _localization);

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
        
        var message = new byte[] { slaveId, selectedFunction.Number, dataByteCount }
            .Concat(data)
            .ToArray();
        
        var result = _modbusMessage.DecodingResponse(
            selectedFunction, 
            CreateAsciiMessageFromBytes(message, true),
            true,
            _localization);
        
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

        var message = new byte[] { slaveId, selectedFunction.Number, declaredByteCount }
            .Concat(data)
            .ToArray();

        Assert.Throws<Exception>(() =>
            _modbusMessage.DecodingResponse(
                selectedFunction, 
                CreateAsciiMessageFromBytes(message, true),
                true,
                _localization));
    }
    
    [Fact]
    public void ReadFunction_ZeroDataLength_Throws()
    {
        const byte slaveId = 1;

        var selectedFunction = Function.ReadCoilStatus;
        
        const byte declaredByteCount = 0;
        
        var data = new byte[] { 0xFF, 0x00 };

        var message = new byte[] { slaveId, selectedFunction.Number, declaredByteCount }
            .Concat(data)
            .ToArray();

        Assert.Throws<Exception>(() =>
            _modbusMessage.DecodingResponse(
                selectedFunction, 
                CreateAsciiMessageFromBytes(message, true),
                true,
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
            // Согласно документации этот код не используется
            if (errorCode == 9)
                continue;
            
            var message = new byte[] { slaveId, (byte)selectedFunctionErrorNumber, errorCode };
        
            var exception = Record.Exception(() => 
                _modbusMessage.DecodingResponse(
                    selectedFunction, 
                    CreateAsciiMessageFromBytes(message, true),
                    true,
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
        
        var message = new byte[] { slaveId, selectedFunction.Number }
            .Concat(address)
            .Concat(data)
            .ToArray();
        
        Assert.Throws<Exception>(() =>
            _modbusMessage.DecodingResponse(
                selectedFunction, 
                CreateAsciiMessageFromBytes(message, true),
                true,
                _localization));
    }

    [Fact]
    public void OddDataByteCount_ReadHoldingRegisters_Throws()
    {
        const byte slaveId = 7;
        
        var selectedFunction = Function.ReadHoldingRegisters;
        
        var data = new byte[] { 0x10 };

        var lengthOfData = (byte)data.Length;
        
        var message = new byte[] { slaveId, selectedFunction.Number, lengthOfData }
            .Concat(data)
            .ToArray();
        
        Assert.Throws<Exception>(() =>
            _modbusMessage.DecodingResponse(
                selectedFunction, 
                CreateAsciiMessageFromBytes(message, true),
                true,
                _localization));
    }
    
    [Fact]
    public void InvalidFunctionNumberInResponse_Throws()
    {
        const byte slaveId = 1;
        
        var expectedFunction = Function.PresetSingleRegister;
        const byte actualFunctionNumber = 0x0A;

        var message = new byte[] { slaveId, actualFunctionNumber, 0x00, 0x01, 0x10, 0xFF };

        Assert.Throws<Exception>(() =>
            _modbusMessage.DecodingResponse(
                expectedFunction, 
                CreateAsciiMessageFromBytes(message, false),
                false, 
                _localization));
    }
    
    [Fact]
    public void CheckSum_DisabledLRC8_Success()
    {
        const byte slaveId = 13;
        
        var selectedFunction = Function.PresetSingleRegister;

        const ushort address = 23;
        const ushort data = 0x337F;

        var addressBytes = new byte[2];
        var dataBytes = new byte[2];

        BinaryPrimitives.WriteUInt16BigEndian(addressBytes, address);
        BinaryPrimitives.WriteUInt16BigEndian(dataBytes, data);
        
        var message = new byte[] { slaveId, selectedFunction.Number }
            .Concat(addressBytes)
            .Concat(dataBytes)
            .ToArray();
        
        var result = _modbusMessage.DecodingResponse(
            selectedFunction, 
            CreateAsciiMessageFromBytes(message, false),
            false,
            _localization);
        
        Assert.Equal(slaveId, result.SlaveID);
        
        Assert.IsType<PduResponseWriteSingle>(result.PDU);
        
        var resultPdu = result.PDU as PduResponseWriteSingle;
        
        Assert.NotNull(resultPdu);
        Assert.Equal(selectedFunction.Number, resultPdu.FunctionNumber);
        Assert.Equal(address, resultPdu.Address);
        Assert.Equal(data, resultPdu.Data);
    }
    
    [Fact]
    public void CheckSum_WrongLRC8_Throws()
    {
        var selectedFunction = Function.PresetSingleRegister;

        byte[] message =
        [
            0x3A,                         // ':'
            0x30, 0x31,                   // '0','1'  SlaveID
            0x30, 0x36,                   // '0','6'  Function code
            0x30, 0x30, 0x30, 0x44,       // '0','0','0','D'  Address = 13
            0x30, 0x38, 0x39, 0x46,       // '0','8','9','F'  Data = 0x089F
            0x33, 0x33,                   // '3','3'  неверный LRC (нужен '4','5')
            0x0D, 0x0A                    // CR LF
        ];

        Assert.Throws<Exception>(() =>
            _modbusMessage.DecodingResponse(selectedFunction, message, true, _localization));
    }
    
    [Fact]
    public void CheckSum_ResponseWithoutLRC8_WhenChecksumEnabled_Throws()
    {
        const byte slaveId = 3;
        
        var selectedFunction = Function.PresetSingleRegister;

        var address = new byte[] { 0x00, 0x01 };
        
        var data = new byte[] { 0x10, 0x56 };
        
        var message = new byte[] { slaveId, selectedFunction.Number }
            .Concat(address)
            .Concat(data)
            .ToArray();
        
        Assert.Throws<Exception>(() =>
            _modbusMessage.DecodingResponse(
                selectedFunction, 
                CreateAsciiMessageFromBytes(message, false),
                true,
                _localization));
    }

    [Fact]
    public void ASCIISpecific_MessageWithoutFrameBytes_Throws()
    {
        var selectedFunction = Function.PresetSingleRegister;

        byte[] message =
        [
            0x31, 0x35,                   // '1','5'  SlaveID
            0x30, 0x36,                   // '0','6'  Function code
            0x30, 0x30, 0x30, 0x44,       // '0','0','0','D'  Address = 13
            0x30, 0x38, 0x39, 0x46,       // '0','8','9','F'  Data = 0x089F
        ];

        Assert.Throws<Exception>(() =>
            _modbusMessage.DecodingResponse(selectedFunction, message, true, _localization));
    }
    
    [Fact]
    public void ASCIISpecific_GarbageInStartAndEnd_Success()
    {
        var selectedFunction = Function.PresetSingleRegister;

        byte[] message =
        [
            0xFD, 0xFA, 0x34, 0x33,       // Мусор
            0x3A,                         // ':'
            0x30, 0x31,                   // '0','1'  SlaveID
            0x30, 0x36,                   // '0','6'  Function code
            0x30, 0x30, 0x30, 0x44,       // '0','0','0','D'  Address = 13
            0x30, 0x38, 0x39, 0x46,       // '0','8','9','F'  Data = 0x089F
            0x34, 0x35,                   // LRC8
            0x0D, 0x0A,                   // CR LF
            0xFD, 0xFA, 0x22              // Мусор
        ];

        var result = _modbusMessage.DecodingResponse(
            selectedFunction, 
            message, 
            true, 
            _localization);
        
        Assert.Equal(1, result.SlaveID);
        
        Assert.IsType<PduResponseWriteSingle>(result.PDU);
        
        var resultPdu = result.PDU as PduResponseWriteSingle;
        
        Assert.NotNull(resultPdu);
        Assert.Equal(selectedFunction.Number, resultPdu.FunctionNumber);
        Assert.Equal(13, resultPdu.Address);
        Assert.Equal(0x089F, resultPdu.Data);
    }
    
    private static byte[] CreateAsciiMessageFromBytes(byte[] mainPart, bool checkSumIsEnable)
    {
        var mainPartAscii = ModbusASCII_Message.ConvertArrayToASCII(mainPart);
        
        var asciiMessage = checkSumIsEnable 
            ? new byte[5 + mainPartAscii.Length]
            : new byte[3 + mainPartAscii.Length];
        
        asciiMessage[0] = ModbusASCII_Message.StartByte;
        
        Array.Copy(mainPartAscii, 0, asciiMessage, 1, mainPartAscii.Length);
        
        if (checkSumIsEnable)
        {
            var lrc8Ascii = CheckSum.Calculate_LRC8_ASCII(mainPart);
            
            asciiMessage[^4] = lrc8Ascii[0];
            asciiMessage[^3] = lrc8Ascii[1];
        }

        asciiMessage[^2] = ModbusASCII_Message.EndByteCR;
        asciiMessage[^1] = ModbusASCII_Message.EndByteLF;

        return asciiMessage;
    }
}