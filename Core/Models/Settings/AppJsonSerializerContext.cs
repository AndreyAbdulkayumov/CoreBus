using Core.Models.AppUpdateSystem.DataTypes;
using Core.Models.Settings.FileTypes;
using System.Text.Json.Serialization;


namespace Core.Models.Settings;

[JsonSerializable(typeof(AppInfo))]
[JsonSerializable(typeof(DeviceData))]
[JsonSerializable(typeof(MacrosModbus))]
[JsonSerializable(typeof(MacrosNoProtocol))]
[JsonSerializable(typeof(ModbusMonitoring))]
[JsonSerializable(typeof(LastestVersionInfo))]
internal partial class SerializerContext : JsonSerializerContext
{

}
