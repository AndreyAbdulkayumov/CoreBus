using System.ComponentModel;
using System.Globalization;
using ViewModels.Validation;

namespace ViewModels.ModbusClient.Manual.WriteFields;

public abstract class ModbusDataFormatter : ValidatedDateInput, INotifyDataErrorInfo
{
    public const string DataFormatName_dec = "dec";
    public const string DataFormatName_hex = "hex";
    public const string DataFormatName_bin = "bin";
    public const string DataFormatName_float = "float";

    public abstract string? ViewData { get; set; }

    public abstract void SetDataFormat(string format);

    protected ushort ConvertStringToNumber(string? value, NumberStyles format)
    {
        if (value == null || value == string.Empty)
        {
            return ushort.MinValue;
        }

        return ushort.Parse(value.Replace(" ", "").Replace("_", ""), format);
    }

    protected string ConvertNumberToString(ushort number, NumberStyles format)
    {
        switch (format)
        {
            case NumberStyles.BinaryNumber:
                return Convert.ToString(number, 2);

            case NumberStyles.Number:
                return Convert.ToString(number, 10);

            case NumberStyles.HexNumber:
                return Convert.ToString(number, 16).ToUpper();

            default:
                throw new ArgumentException("Неподдерживаемый формат числа.");
        }
    }
}
