using System.Text.Encodings.Web;
using System.Text.Json;

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
            throw new Exception($"Не удалось найти тип {typeof(T)} в объявлении контекста сериализации.");
        }

        JsonSerializer.Serialize(stream, data, jsonTypeInfo);
    }

    public static T? Read<T>(string filePath)
    {
        string correctFilePath = GetCorrectFilePath(filePath);

        if (!File.Exists(correctFilePath))
        {
            throw new Exception("Файл настроек не существует.\n\n" + "Путь: " + correctFilePath);
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
            throw new Exception($"Файл с именем \"{Path.GetFileName(destFileName)}\" уже существует в директории \"{Path.GetDirectoryName(destFileName)}\".");
        }

        File.Copy(GetCorrectFilePath(sourceFileName), destFileName);
    }

    public static void Delete(string fileName)
    {
        if (!File.Exists(fileName))
        {
            throw new Exception($"Файла \"{fileName}\" не существует.");
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
