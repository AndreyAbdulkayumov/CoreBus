using System.Globalization;

namespace ViewModels.Validation;

public static class StringValue
{
    public static bool IsValidNumber(string stringNumber, NumberStyles style, out byte number)
    {
        return byte.TryParse(stringNumber.Replace(" ", string.Empty), style, CultureInfo.InvariantCulture, out number);
    }

    public static bool IsValidNumber(string stringNumber, NumberStyles style, out UInt16 number)
    {
        return UInt16.TryParse(stringNumber.Replace(" ", string.Empty), style, CultureInfo.InvariantCulture, out number);
    }
    
    public static bool IsValidNumber(string stringNumber, NumberStyles style, out int number)
    {
        return int.TryParse(stringNumber.Replace(" ", string.Empty), style, CultureInfo.InvariantCulture, out number);
    }
    
    public static bool IsValidNumber(string stringNumber, NumberStyles style, out uint number)
    {
        return uint.TryParse(stringNumber.Replace(" ", string.Empty), style, CultureInfo.InvariantCulture, out number);
    }

    public static bool IsValidNumber(string stringNumber, NumberStyles style, out float number)
    {
        return float.TryParse(stringNumber.Replace(" ", string.Empty), style, CultureInfo.InvariantCulture, out number);
    }
}
