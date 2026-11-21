namespace Core.Models.Modbus.DataTypes;

public class UnexpectedSlaveIdException : Exception
{
    public override string Message { get; }
    
    public readonly byte ExpectedSlaveId;
    public readonly byte ActualSlaveId;

    public UnexpectedSlaveIdException(string message, byte expectedSlaveId, byte actualSlaveId)
    {
        Message = message;
        ExpectedSlaveId = expectedSlaveId;
        ActualSlaveId = actualSlaveId;
    }
}