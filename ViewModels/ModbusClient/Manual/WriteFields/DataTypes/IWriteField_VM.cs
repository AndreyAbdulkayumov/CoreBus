using Core.Models.Settings.DataTypes;
using ViewModels.ModbusClient.Manual.DataTypes;

namespace ViewModels.ModbusClient.Manual.WriteFields.DataTypes;

public interface IWriteField_VM
{
    WriteData GetData();
    void SetDataFromMacros(ModbusMacrosWriteInfo data);
    ModbusMacrosWriteInfo GetMacrosData();
    bool HasValidationErrors { get; }
    string? ValidationMessage { get; }
}
