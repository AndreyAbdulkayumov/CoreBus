namespace Core.Models.Modbus.DataTypes;

public class UnexpectedSlaveIdException : Exception
{
    public readonly byte ExpectedSlaveId;
    public readonly byte ActualSlaveId;
    
    public UnexpectedSlaveIdException(string message, byte expectedSlaveId, byte actualSlaveId) 
        : base(message)
    {
        ExpectedSlaveId = expectedSlaveId;
        ActualSlaveId = actualSlaveId;
    }

    public UnexpectedSlaveIdException(UnexpectedSlaveIdException exception, ModbusExceptionInfo? innerException = null) 
        : base(exception.Message, innerException)
    {
        ExpectedSlaveId = exception.ExpectedSlaveId;
        ActualSlaveId = exception.ActualSlaveId;
    }
}