namespace Core.Models.Modbus.DataTypes;

public readonly record struct ExpectedResponseData(byte SlaveId, bool CheckSumIsEnable);
