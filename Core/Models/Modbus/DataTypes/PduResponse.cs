namespace Core.Models.Modbus.DataTypes;

public abstract record PduResponse(int FunctionNumber);

public record PduResponseRead(int FunctionNumber, byte[] Data) : PduResponse(FunctionNumber);

public record PduResponseWriteSingle(int FunctionNumber, ushort Address, ushort Data) : PduResponse(FunctionNumber);

public record PduResponseWriteMultiple(int FunctionNumber, ushort Address, ushort RegisterCount) : PduResponse(FunctionNumber);
