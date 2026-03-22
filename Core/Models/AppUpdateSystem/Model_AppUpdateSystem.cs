using Core.Models.AppUpdateSystem.DataTypes;
using Core.Models.Settings;
using System.Diagnostics;
using System.Text.Json;

namespace Core.Models.AppUpdateSystem;

public class Model_AppUpdateSystem
{
    private const string UrlLastestVersion = "https://andreyabdulkayumov.github.io/TerminalProgram_Website/lastestVersion.json";

    private const string UrlDonate = "https://andreyabdulkayumov.github.io/TerminalProgram_Website/donate.html";

    private const string UrlVideo = "https://andreyabdulkayumov.github.io/TerminalProgram_Website/video.html";

    public Model_AppUpdateSystem()
    {

    }

    /// <summary>
    /// Проверяет наличие новой версии приложения. Если используется актуальная версия приложения, то возвращается null.
    /// </summary>
    /// <param name="currentVersion"></param>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    public async Task<LastestVersionInfo?> IsUpdateAvailable(Version currentVersion)
    {
        var client = new HttpClient();

        var response = await client.GetStringAsync(UrlLastestVersion);

        var info = JsonSerializer.Deserialize<LastestVersionInfo>(response, SerializerContext.Default.LastestVersionInfo);

        if (info != null && !string.IsNullOrEmpty(info.Version))
        {
            var lastestVersion = new Version(info.Version);

            if (lastestVersion > currentVersion)
            {
                return info;
            }

            return null;
        }

        throw new Exception("Нарушена целостность файла с информацией о последней версии приложения.");
    }

    /// <summary>
    /// Переход на страницу пожертвований
    /// </summary>
    public void GoToDonatePage()
    {
        GoToWebPage(UrlDonate);
    }

    public void GoToVideoPage()
    {
        GoToWebPage(UrlVideo);
    }

    /// <summary>
    /// Переход на страницу по заданному URL.
    /// </summary>
    /// <param name="url"></param>
    public void GoToWebPage(string? url)
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = url,
            UseShellExecute = true // Позволяет открыть URL в стандартном браузере
        });
    }

    public string GetDownloadLink(LastestVersionInfo info)
    {
        string? downloadLink = null;

        if (OperatingSystem.IsWindows())
        {
            if (!string.IsNullOrEmpty(info.DownloadLink_Windows))
            {
                downloadLink = info.DownloadLink_Windows;
            }
        }

        else if (OperatingSystem.IsLinux())
        {
            if (!string.IsNullOrEmpty(info.DownloadLink_Linux))
            {
                downloadLink = info.DownloadLink_Linux;
            }
        }

        if (string.IsNullOrEmpty(downloadLink))
        {
            throw new Exception("Не удалось получить ссылку на скачивание новой версии приложения.");
        }

        return downloadLink;
    }
}
