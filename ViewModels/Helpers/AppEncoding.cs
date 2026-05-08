using System.Text;
using Services.Interfaces;

namespace ViewModels.Helpers;

public static class AppEncoding
{
    public const string Name_ASCII = "ASCII";
    public const string Name_Unicode = "Unicode";
    public const string Name_UTF32 = "UTF-32";
    public const string Name_UTF8 = "UTF-8";

    public static Encoding GetEncoding(string? encodingName)
    {
        switch (encodingName)
        {
            case Name_ASCII:
                return Encoding.ASCII;

            case Name_Unicode:
                return Encoding.Unicode;

            case Name_UTF32:
                return Encoding.UTF32;

            case Name_UTF8:
                return Encoding.UTF8;

            default:
                throw new Exception(LocalizationProvider.Get("Exception.UnknownEncoding", encodingName ?? string.Empty));
        }
    }
}
