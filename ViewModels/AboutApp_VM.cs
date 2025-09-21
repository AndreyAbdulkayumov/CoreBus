using Core.Models.AppUpdateSystem;
using Core.Models.AppUpdateSystem.DataTypes;
using MessageBox.Core;
using ReactiveUI;
using Services.Interfaces;
using System.Reactive;

namespace ViewModels;

public class AboutApp_VM : ReactiveObject
{
    private const string _defaultVersionValue = "0.0.0";

    private Version? _appVersionFull { get; set; }

    private string? _appVersion = _defaultVersionValue;

    public string? AppVersion
    {
        get => _appVersion;
        set => this.RaiseAndSetIfChanged(ref _appVersion, value);
    }

    private string? _avaloniaVersion = _defaultVersionValue;

    public string? AvaloniaVersion
    {
        get => _avaloniaVersion;
        set => this.RaiseAndSetIfChanged(ref _avaloniaVersion, value);
    }

    private string? _runtimeVersion = _defaultVersionValue;

    public string? RuntimeVersion
    {
        get => _runtimeVersion;
        set => this.RaiseAndSetIfChanged(ref _runtimeVersion, value);
    }

    public ReactiveCommand<string, Unit> Command_Copy_SourceCodeRepositoryAddress { get; }
    public ReactiveCommand<string, Unit> Command_Copy_Email { get; }
    public ReactiveCommand<Unit, Unit>? Command_CheckUpdate { get; }
    public ReactiveCommand<Unit, Unit> Command_MakeDonate { get; }

    private readonly IMessageBox _messageBox;
    private readonly IUIService _uiService;
    private readonly Model_AppUpdateSystem _appUpdateSystemModel;

    public AboutApp_VM(IMessageBoxAboutApp messageBox, IUIService uiService,
        Model_AppUpdateSystem appUpdateSystemModel)
    {
        _messageBox = messageBox ?? throw new ArgumentNullException(nameof(messageBox));
        _uiService = uiService ?? throw new ArgumentNullException(nameof(uiService));
        _appUpdateSystemModel = appUpdateSystemModel ?? throw new ArgumentNullException(nameof(appUpdateSystemModel));

        Command_Copy_SourceCodeRepositoryAddress = ReactiveCommand.CreateFromTask<string>(uiService.CopyToClipboard);
        Command_Copy_SourceCodeRepositoryAddress.ThrownExceptions.Subscribe(error => _messageBox.Show($"Ошибка копирования ссылки на исходный код:\n\n{error.Message}", MessageType.Error, error));

        Command_Copy_Email = ReactiveCommand.CreateFromTask<string>(uiService.CopyToClipboard);
        Command_Copy_Email.ThrownExceptions.Subscribe(error => _messageBox.Show($"Ошибка копирования почты автора:\n\n{error.Message}", MessageType.Error, error));

        Command_CheckUpdate = ReactiveCommand.CreateFromTask(CheckUpdate);
        Command_CheckUpdate.ThrownExceptions.Subscribe(error => _messageBox.Show($"Ошибка проверки обновлений:\n\n{error.Message}", MessageType.Error, error));

        Command_MakeDonate = ReactiveCommand.Create(_appUpdateSystemModel.GoToDonatePage);
        Command_MakeDonate.ThrownExceptions.Subscribe(error => _messageBox.Show($"Ошибка :(\n\n{error.Message}", MessageType.Error, error));

        _appVersionFull = _uiService.GetAppVersion();

        _appVersion = _appVersionFull?.ToString();
        _avaloniaVersion = _uiService.GetAvaloniaVersionString();
        _runtimeVersion = _uiService.GetRuntimeVersion().ToString();
    }

    private async Task CheckUpdate()
    {
        if (_appVersionFull == null)
        {
            return;
        }

        LastestVersionInfo? info = await _appUpdateSystemModel.IsUpdateAvailable(_appVersionFull);

        if (info != null)
        {
            if (!string.IsNullOrEmpty(info.Version))
            {
                string downloadLink = _appUpdateSystemModel.GetDownloadLink(info);

                if (await _messageBox.ShowYesNoDialog($"Доступна новая версия приложения - {info.Version}\nПерейти на страницу скачивания?", MessageType.Information) == MessageBoxResult.Yes)
                {
                    _appUpdateSystemModel.GoToWebPage(downloadLink);
                }
                return;
            }

            throw new Exception("Нарушена целостность файла с информацией об обновлении.");
        }

        _messageBox.Show("Вы используйте актуальную версию приложения!", MessageType.Information);
    }
}
