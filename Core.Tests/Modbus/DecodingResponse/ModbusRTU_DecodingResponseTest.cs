using Core.Models.Modbus;
using Core.Models.Modbus.DataTypes;
using Core.Models.Modbus.Message;
using Core.Tests.Infrastructure;

namespace Core.Tests.Modbus.DecodingResponse;

public class ModbusRTU_DecodingResponseTest
{
    private readonly ModbusMessage _modbusMessage = new ModbusRTU_Message();
    private readonly ILocalizationService _localization = new TestLocalizationService();
    
    [Fact]
    public void WriteFunction_Success()
    {
        const byte slaveId = 1;
        
        var selectedFunction = Function.PresetSingleRegister;

        var address = new byte[] { 0x00, 0x01 };
        var data = new byte[] { 0x10, 0xFF };
        
        var message = new byte[] { slaveId, selectedFunction.Number }
            .Concat(address)
            .Concat(data)
            .ToArray();
        
        var result = _modbusMessage.DecodingResponse(selectedFunction, GetMessageWithCheckSum(message), true, _localization);
        
        Assert.Equal(slaveId, result.SlaveID);
        Assert.Equal(selectedFunction.Number, result.Command);
    }
    
    [Fact]
    public void PresetSingleRegister_ShortResponse_Throws()
    {
        const byte slaveId = 1;
        
        var selectedFunction = Function.PresetSingleRegister;

        var address = new byte[] { 0x00, 0x01 };
        
        var data = new byte[] { 0x10 };
        
        var message = new byte[] { slaveId, selectedFunction.Number }
            .Concat(address)
            .Concat(data)
            .ToArray();
        
        Assert.Throws<Exception>(() => _modbusMessage.DecodingResponse(selectedFunction, GetMessageWithCheckSum(message), true, _localization));
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
        
        var result = _modbusMessage.DecodingResponse(selectedFunction, GetMessageWithCheckSum(message), true, _localization);
        
        Assert.Equal(slaveId, result.SlaveID);
        Assert.Equal(selectedFunction.Number, result.Command);
        Assert.Equal(dataByteCount, result.LengthOfData);
        Assert.Equal(data, result.Data);
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
            _modbusMessage.DecodingResponse(selectedFunction, GetMessageWithCheckSum(message), true, _localization));
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
            _modbusMessage.DecodingResponse(selectedFunction, GetMessageWithCheckSum(message), true, _localization));
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
            
            var message = new byte[] { slaveId, (byte)selectedFunctionErrorNumber, errorCode };
        
            var exception = Record.Exception(() => 
                _modbusMessage.DecodingResponse(selectedFunction, GetMessageWithCheckSum(message), true, _localization));
        
            Assert.IsType<ModbusException>(exception);
        }
    }
    
    [Fact]
    public void CheckSum_DisabledCRC_Success()
    {
        const byte slaveId = 1;
        
        var selectedFunction = Function.PresetSingleRegister;

        var address = new byte[] { 0x00, 0x01 };
        var data = new byte[] { 0x10, 0xFF };
        
        var message = new byte[] { slaveId, selectedFunction.Number }
            .Concat(address)
            .Concat(data)
            .ToArray();
        
        var result = _modbusMessage.DecodingResponse(selectedFunction, message, false, _localization);
        
        Assert.Equal(slaveId, result.SlaveID);
        Assert.Equal(selectedFunction.Number, result.Command);
    }
    
    [Fact]
    public void CheckSum_WrongCRC_Throws()
    {
        const byte slaveId = 10;
        var selectedFunction = Function.PresetSingleRegister;

        var message = new byte[] { slaveId, selectedFunction.Number, 0x00, 0x01, 0x10, 0xFF, 0x33, 0x12 };

        Assert.Throws<Exception>(() =>
            _modbusMessage.DecodingResponse(selectedFunction, message, true, _localization));
    }
    
    [Fact]
    public void CheckSum_ResponseWithoutCRC_WhenChecksumEnabled_Throws()
    {
        const byte slaveId = 1;
        var selectedFunction = Function.PresetSingleRegister;

        var message = new byte[] { slaveId, selectedFunction.Number, 0x00, 0x01, 0x10, 0xFF };

        Assert.Throws<Exception>(() =>
            _modbusMessage.DecodingResponse(selectedFunction, message, true, _localization));
    }
    
    private static byte[] GetMessageWithCheckSum(byte[] data)
    {
        var message = new byte[data.Length + 2];
        Array.Copy(data, message, data.Length);

        var crc = CheckSum.Calculate_CRC16(message);
        message[^2] = crc[0];
        message[^1] = crc[1];

        return message;
    }
}