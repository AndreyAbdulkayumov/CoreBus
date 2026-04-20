using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Reactive;
using MessageBox.Core;
using Core.Models.Settings;
using Core.Models.Settings.FileTypes;
using Services.Interfaces;

namespace ViewModels.Settings.Tabs;

public class AppSettings_VM : ReactiveObject
{
    public ReactiveCommand<Unit, Unit> Select_Dark_Theme { get; }
    public ReactiveCommand<Unit, Unit> Select_Light_Theme { get; }

    private bool _checkAppUpdateAfterStart;

    public bool CheckAppUpdateAfterStart
    {
        get => _settingsModel.AppData.CheckUpdateAfterStart;
        set
        {
            _settingsModel.AppData.CheckUpdateAfterStart = value;
            this.RaiseAndSetIfChanged(ref _checkAppUpdateAfterStart, value);
        }
    }

    /// <summary>
    /// Список языков, загруженных сервисом локализации.
    /// Пополняется автоматически при добавлении нового JSON-файла в Localization/.
    /// </summary>
    public IReadOnlyList<LanguageInfo> Languages => _localization.AvailableLanguages;

    /// <summary>
    /// Текущий выбранный язык. Смена значения сразу переключает язык интерфейса
    /// и сохраняет выбор в настройках приложения.
    /// </summary>
    public LanguageInfo SelectedLanguage
    {
        get => _localization.CurrentLanguage;
        set
        {
            if (value is null) return;
            if (string.Equals(value.Code, _localization.CurrentLanguage.Code, StringComparison.OrdinalIgnoreCase))
                return;

            _localization.SetLanguage(value.Code);
            _settingsModel.AppData.LanguageCode = value.Code;
            this.RaisePropertyChanged();
        }
    }

    private readonly IUIService _uiServices;
    private readonly IMessageBoxSettings _messageBox;
    private readonly Model_Settings _settingsModel;
    private readonly ILocalizationService _localization;

    public AppSettings_VM(IUIService uiServices,
                          IMessageBoxSettings messageBox,
                          Model_Settings settingsModel,
                          ILocalizationService localization)
    {
        _uiServices = uiServices ?? throw new ArgumentNullException(nameof(uiServices));
        _messageBox = messageBox ?? throw new ArgumentNullException(nameof(messageBox));
        _settingsModel = settingsModel ?? throw new ArgumentNullException(nameof(settingsModel));
        _localization = localization ?? throw new ArgumentNullException(nameof(localization));

        Select_Dark_Theme = ReactiveCommand.Create(SetDarkTheme);
        Select_Dark_Theme.ThrownExceptions.Subscribe(error =>
            _messageBox.Show(
                _localization.Get("Error.ThemeSwitchDark") + "\n\n" + error.Message,
                MessageType.Error, error));

        Select_Light_Theme = ReactiveCommand.Create(SetLightTheme);
        Select_Light_Theme.ThrownExceptions.Subscribe(error =>
            _messageBox.Show(
                _localization.Get("Error.ThemeSwitchLight") + "\n\n" + error.Message,
                MessageType.Error, error));

        // При смене языка перечитать все свойства, зависящие от локализации.
        _localization.LanguageChanged += (_, _) =>
        {
            this.RaisePropertyChanged(nameof(SelectedLanguage));
        };
    }

    private void SetDarkTheme()
    {
        _uiServices.Set_Dark_Theme();

        _settingsModel.AppData.ThemeName = AppTheme.Dark;
    }

    private void SetLightTheme()
    {
        _uiServices.Set_Light_Theme();

        _settingsModel.AppData.ThemeName = AppTheme.Light;
    }
}
