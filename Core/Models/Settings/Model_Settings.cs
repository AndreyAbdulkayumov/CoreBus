using System.Globalization;
using Core.Models.Settings.DataTypes;
using Core.Models.Settings.FileTypes;

namespace Core.Models.Settings;

public class Model_Settings
{
    public DeviceData? Settings { get; private set; }

    public AppInfo AppData { get; private set; }

    public ModbusMonitoringParameters ModbusMonitoringItems { get; private set; }

    /// <summary>
    /// Путь к папке с файлами настроек
    /// </summary>
    public string FolderPath_Settings
    {
        get => DirectoryManager.SettingsFiles_Directory;
    }

    public string FilePath_Macros_NoProtocol
    {
        get => Path.Combine(DirectoryManager.Macros_Directory, FileName_Macros_NoProtocol + FileExtension);
    }

    public string FilePath_Macros_Modbus
    {
        get => Path.Combine(DirectoryManager.Macros_Directory, FileName_Macros_Modbus + FileExtension);
    }

    public string LogFolderPath => DirectoryManager.LogFiles_Directory;

    private const string FileName_DefaultPreset = "Unknown";

    private const string FileName_AppData = "AppData";

    private const string FileName_ModbusMonitoringItems = "ModbusMonitoringItems";

    private const string FileName_Macros_NoProtocol = "Macros_NoProtocol";
    private const string FileName_Macros_Modbus = "Macros_Modbus";

    private const string FileExtension = ".json";

    private readonly AppDirectoryManager DirectoryManager = new AppDirectoryManager();
    
    private readonly ILocalizationService _localization;

    public Model_Settings(ILocalizationService localization)
    {
        _localization = localization ?? throw new ArgumentNullException(nameof(localization));
        
        AppData = ReadAppInfo();

        ModbusMonitoringItems = ReadModbusMonitoringItems();
    }

    /// <summary>
    /// Сохранение данных в файл. Если файл с таким именем не найден, то он создается.
    /// </summary>
    /// <param name="fileName"></param>
    /// <param name="data"></param>
    /// <exception cref="Exception"></exception>
    public void SavePreset(string fileName, DeviceData data)
    {
        try
        {
            if (fileName == string.Empty)
            {
                throw new Exception(_localization.Get("Core.SettingsFileNameNotSet"));
            }

            string filePath = Path.Combine(FolderPath_Settings, fileName + FileExtension);

            FileIO.Save(filePath, data);

            Settings = (DeviceData)data.Clone();
        }

        catch (Exception error)
        {
            throw new Exception(_localization.Get("Core.SettingsSaveError") + "\n\n" + error.Message);
        }
    }

    /// <summary>
    /// Чтение настроек из файла. Если файл содержит битые данные, то он перезаписывается значениями по умолчанию.
    /// </summary>
    /// <param name="fileName"></param>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    public DeviceData ReadPreset(string fileName)
    {
        try
        {
            if (fileName == string.Empty)
            {
                throw new Exception(_localization.Get("Core.SettingsFileNameNotSet"));
            }

            string filePath = Path.Combine(FolderPath_Settings, fileName + FileExtension);

            Settings = FileIO.ReadOrCreateDefault(filePath, DeviceData.GetDefault());

            return Settings;
        }

        catch (Exception error)
        {
            throw new Exception(_localization.Get("Core.SettingsReadErrorDetailed") + "\n\n" + error.Message);
        }
    }

    /// <summary>
    /// Удаляет файл по указанному пути, если он существует.
    /// </summary>
    /// <param name="fileName"></param>
    public void DeleteFile(string fileName)
    {
        FileIO.Delete(fileName);
    }

    /// <summary>
    /// Удаляет файл пресета по указанному пути, если он существует.
    /// </summary>
    /// <param name="fileName"></param>
    public void DeletePreset(string fileName)
    {
        string[] arrayOfFiles = Directory.GetFiles(DirectoryManager.SettingsFiles_Directory);

        string selectedFile_FullPath = Path.Combine(DirectoryManager.SettingsFiles_Directory, fileName + FileExtension);

        if (arrayOfFiles.Contains(selectedFile_FullPath))
        {
            FileIO.Delete(Path.Combine(DirectoryManager.SettingsFiles_Directory, fileName) + FileExtension);
        }

        else
        {
            throw new Exception(_localization.Get("Core.FileNotFoundInFolder", fileName, DirectoryManager.SettingsFiles_Directory));
        }
    }

    /// <summary>
    /// Копирование файла из одной директории в другую.
    /// </summary>
    /// <param name="sourceFileName"></param>
    /// <param name="destFileName"></param>
    public void CopyFile(string sourceFileName, string destFileName)
    {
        FileIO.Copy(sourceFileName, destFileName);
    }

    /// <summary>
    /// Копирует файл с указанным путем в директорию пресетов.
    /// </summary>
    /// <param name="filePath"></param>
    /// <returns>Имя скопированного файла без расширения.</returns>
    public string CopyInPresetFolderFrom(string filePath)
    {
        string destFilePath = Path.Combine(DirectoryManager.SettingsFiles_Directory, Path.GetFileName(filePath));

        string fileName = Path.GetFileNameWithoutExtension(filePath);

        FileIO.Copy(filePath, destFilePath);

        return fileName;
    }

    /// <summary>
    /// Возвращает имена всех доступных файлов настроек.
    /// </summary>
    /// <returns></returns>
    public string[] FindFilesOfPresets()
    {
        string[] arrayOfPresets = DirectoryManager.CheckFiles(
            DirectoryManager.SettingsFiles_Directory,
            FileName_DefaultPreset,
            FileExtension,
            DeviceData.GetDefault()
            );

        return arrayOfPresets;
    }

    /// <summary>
    /// Сохранение файла настроек приложения
    /// </summary>
    /// <param name="data"></param>
    public void SaveAppInfo(AppInfo data)
    {
        try
        {
            string filePath = Path.Combine(DirectoryManager.CommonFiles_Directory, FileName_AppData + FileExtension);

            FileIO.Save(filePath, data);

            AppData = data;
        }

        catch (Exception error)
        {
            throw new Exception(_localization.Get("Core.AppSettingsSaveError") + "\n\n" + error.Message);
        }
    }

    /// <summary>
    /// Чтение настроек приложения из AppData.json. При первом запуске подставляется код языка по UI-локали.
    /// </summary>
    /// <returns>Модель настроек приложения.</returns>
    private AppInfo ReadAppInfo()
    {
        var appDataPath = Path.Combine(DirectoryManager.CommonFiles_Directory, FileName_AppData + FileExtension);
        var defaults = AppInfo.GetDefault(FileName_DefaultPreset);

        if (!File.Exists(appDataPath))
        {
            defaults.LanguageCode = _localization.GetLanguageCodeFromCurrentCulture();
        }

        var path = DirectoryManager.FindOrCreateFile(
            DirectoryManager.CommonFiles_Directory,
            FileName_AppData,
            FileExtension,
            defaults);

        return FileIO.ReadOrCreateDefault(path, defaults);
    }

    /// <summary>
    /// Сохранение Modbus регистров мониторинга
    /// </summary>
    /// <param name="data"></param>
    /// <exception cref="Exception"></exception>
    public void SaveModbusMonitoringItems(ModbusMonitoringParameters data)
    {
        try
        {
            string filePath = Path.Combine(DirectoryManager.CommonFiles_Directory, FileName_ModbusMonitoringItems + FileExtension);

            FileIO.Save(filePath, data);

            ModbusMonitoringItems = data;
        }

        catch (Exception error)
        {
            throw new Exception(_localization.Get("Core.MonitoringRegistersSaveError") + "\n\n" + error.Message);
        }
    }

    /// <summary>
    /// Чтение сохраненых Modbus регистров мониторинга
    /// </summary>
    /// <returns></returns>
    private ModbusMonitoringParameters ReadModbusMonitoringItems()
    {
        string filePath = DirectoryManager.FindOrCreateFile(
            DirectoryManager.CommonFiles_Directory,
            FileName_ModbusMonitoringItems,
            FileExtension,
            ModbusMonitoringParameters.GetDefault()
            );

        return FileIO.ReadOrCreateDefault(filePath, ModbusMonitoringParameters.GetDefault());
    }

    /// <summary>
    /// Сохранение макросов
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="macros"></param>
    public void SaveMacros<T>(T macros)
    {
        string filePath = GetMacrosFilePath<T>(out _);

        FileIO.Save(filePath, macros);
    }

    /// <summary>
    /// Чтение файла макросов
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="filePath"></param>
    /// <returns></returns>
    public T ReadMacros<T>(string filePath)
    {
        var macros = FileIO.Read<T>(filePath);

        if (macros == null)
        {
            throw new Exception(_localization.Get("Exception.FileReadError"));
        }

        return macros;
    }

    /// <summary>
    /// Чтение файла макросов или создание файла по умолчанию
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public T ReadOrCreateDefaultMacros<T>()
    {
        string filePath = GetMacrosFilePath<T>(out object defaultMacrosValue);

        return FileIO.ReadOrCreateDefault(filePath, (T)defaultMacrosValue);
    }

    private string GetMacrosFilePath<T>(out object defaultMacrosValue)
    {
        string fileName;

        if (typeof(T) == typeof(MacrosModbus))
        {
            fileName = FileName_Macros_Modbus;
            defaultMacrosValue = new MacrosModbus()
            {
                Items = new List<MacrosContent<ModbusAdditionalData, MacrosCommandModbus>>()
            };
        }

        else if (typeof(T) == typeof(MacrosNoProtocol))
        {
            fileName = FileName_Macros_NoProtocol;
            defaultMacrosValue = new MacrosNoProtocol()
            {
                Items = new List<MacrosContent<object, MacrosCommandNoProtocol>>()
            };
        }

        else
        {
            throw new Exception(_localization.Get("Core.UnsupportedMacrosTypeSaveAttempt"));
        }

        return DirectoryManager.FindOrCreateFile(
            DirectoryManager.Macros_Directory,
            fileName,
            FileExtension,
            defaultMacrosValue
            );
    }

    /// <summary>
    /// Получение списка имен файлов для отправки в режиме "Без протокола"
    /// </summary>
    /// <returns>Возвращает список имен файлов с расширениями</returns>
    public IEnumerable<string> GetAllSendFilesNames()
    {
        return DirectoryManager.GetFilesFromDirectory(DirectoryManager.SendFiles_Directory);
    }

    /// <summary>
    /// Копирование файла для отправки в режиме "Без протокола" в специальную директорию
    /// </summary>
    /// <param name="sourceFileName"></param>
    /// <returns>Возвращает путь к созданному файлу</returns>
    public string CopySendFileInWorkDirectory(string sourceFileName)
    {
        string targetFilePath = Path.Combine(DirectoryManager.SendFiles_Directory, Path.GetFileName(sourceFileName));

        CopyFile(sourceFileName, targetFilePath);

        return targetFilePath;
    }
}
