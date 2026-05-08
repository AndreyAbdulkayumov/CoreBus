using Core.Models.Settings.FileTypes;
using Services.Interfaces;

namespace ViewModels.Helpers.FloatNumber;

public static class FloatHelper
{
    public static byte[] GetBytesFromFloatNumber(float floatNumber, FloatNumberFormat floatFormat)
    {
        byte[] bytes = BitConverter.GetBytes(floatNumber);

        Array.Reverse(bytes); // т.к. в протоколе Modbus используется передача данных старшим байтом вперед.

        return GetFormattedBytes(bytes, floatFormat);
    }

    public static float GetFloatNumberFromBytes(byte[] bytes, FloatNumberFormat floatFormat)
    {
        return BitConverter.ToSingle(GetFormattedBytes(bytes, floatFormat), 0);
    }

    public static FloatNumberFormat GetFloatNumberFormatOrDefault(string? formatName)
    {
        switch (formatName)
        {
            case DeviceData.FloatWriteFormat_AB_CD:
                return FloatNumberFormat.AB_CD;

            case DeviceData.FloatWriteFormat_BA_DC:
                return FloatNumberFormat.BA_DC;

            case DeviceData.FloatWriteFormat_CD_AB:
                return FloatNumberFormat.CD_AB;

            case DeviceData.FloatWriteFormat_DC_BA:
                return FloatNumberFormat.DC_BA;

            default:
                return FloatNumberFormat.BA_DC;
        }
    }

    private static byte[] GetFormattedBytes(byte[] bytes, FloatNumberFormat floatFormat)
    {
        switch (floatFormat)
        {
            case FloatNumberFormat.AB_CD:
                return [bytes[1], bytes[0], bytes[3], bytes[2]];

            case FloatNumberFormat.BA_DC:
                return [bytes[0], bytes[1], bytes[2], bytes[3]];

            case FloatNumberFormat.CD_AB:
                return [bytes[3], bytes[2], bytes[1], bytes[0]];

            case FloatNumberFormat.DC_BA:
                return [bytes[2], bytes[3], bytes[0], bytes[1]];

            default:
                throw new Exception(LocalizationProvider.Get("Exception.UnknownFloatFormat"));
        }
    }
}
