using System.Text.Encodings.Web;
using System.Text.Json;
using Services.Interfaces;

namespace Core.Models.Settings;

internal static class FileIO
{
    public static void Save<T>(string filePath, T data)
    {
        string correctFilePath = GetCorrectFilePath(filePath);

        if (!File.Exists(correctFilePath))
        {
            File.Create(correctFilePath).Close();
        }

        File.WriteAllText(correctFilePath, string.Empty);

        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            TypeInfoResolver = SerializerContext.Default
        };

        using var stream = new FileStream(correctFilePath, FileMode.Open);

        var jsonTypeInfo = options.TypeInfoResolver.GetTypeInfo(typeof(T), options);

        if (jsonTypeInfo == null)
        {
            throw new Exception(LocalizationProvider.Get("Core.SerializationTypeNotFound", typeof(T)));
        }

        JsonSerializer.Serialize(stream, data, jsonTypeInfo);
    }

    public static T? Read<T>(string filePath)
    {
        string correctFilePath = GetCorrectFilePath(filePath);

        if (!File.Exists(correctFilePath))
        {
            throw new Exception(LocalizationProvider.Get("Core.SettingsFileNotExistsPath", correctFilePath));
        }

        T? data;

        try
        {
            using var stream = new FileStream(correctFilePath, FileMode.Open);

            data = (T?)JsonSerializer.Deserialize(stream, typeof(T), SerializerContext.Default);
        }

        // Если в файле некоректные данные.
        catch (JsonException)
        {
            return default;
        }

        return data;
    }

    public static T ReadOrCreateDefault<T>(string filePath, T defaultData)
    {
        try
        {
            string correctFilePath = GetCorrectFilePath(filePath);

            T? data = Read<T>(correctFilePath);

            if (data == null)
            {
                Save(correctFilePath, defaultData);

                data = defaultData;
            }

            return data;
        }

        catch (Exception)
        {
            return defaultData;
        }
    }

    public static void Copy(string sourceFileName, string destFileName)
    {
        if (File.Exists(destFileName))
        {
            throw new Exception(LocalizationProvider.Get("Core.FileAlreadyExistsInDirectory", Path.GetFileName(destFileName), Path.GetDirectoryName(destFileName) ?? string.Empty));
        }

        File.Copy(GetCorrectFilePath(sourceFileName), destFileName);
    }

    public static void Delete(string fileName)
    {
        if (!File.Exists(fileName))
        {
            throw new Exception(LocalizationProvider.Get("Core.FileNotExists", fileName));
        }

        File.Delete(fileName);
    }

    public static string GetCorrectFilePath(string fileName)
    {
        string correctFileName = Uri.UnescapeDataString(fileName);

        // Нормализация разделителей пути под текущую ОС
        correctFileName = correctFileName
            .Replace('/', Path.DirectorySeparatorChar)
            .Replace('\\', Path.DirectorySeparatorChar);

        return Uri.UnescapeDataString(correctFileName);
    }
}
