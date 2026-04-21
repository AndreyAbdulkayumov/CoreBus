using Core.Models;
using ReactiveUI;
using Services.Interfaces;
using ViewModels.Validation;

namespace ViewModels.ModbusClient.Monitoring;

public class EditFormula_VM : ValidatedDateInput
{
    private bool _ui_IsEnable;

    public bool UI_IsEnable
    {
        get => _ui_IsEnable;
        set => this.RaiseAndSetIfChanged(ref _ui_IsEnable, value);
    }

    private string? _windowTitle;

    public string? WindowTitle
    {
        get => _windowTitle;
        set => this.RaiseAndSetIfChanged(ref _windowTitle, value);
    }

    private string? _description;

    public string? Description
    {
        get => _description;
        set => this.RaiseAndSetIfChanged(ref _description, value);
    }

    private string? _formula;

    public string? Formula
    {
        get => _formula;
        set
        {
            this.RaiseAndSetIfChanged(ref _formula, value);
            ValidateInput(nameof(Formula), value);
        }
    }

    private bool _saveIsEnabled;

    public bool SaveIsEnabled
    {
        get => _saveIsEnabled;
        set => this.RaiseAndSetIfChanged(ref _saveIsEnabled, value);
    }


    private bool _saved;

    private readonly ILocalizationService _localization;

    public EditFormula_VM(ILocalizationService localization)
    {
        _localization = localization ?? throw new ArgumentNullException(nameof(localization));

        ErrorsChanged += EditFormula_VM_ErrorsChanged;

        _localization.LanguageChanged += (_, _) => UpdateLocalizedStrings();
    }

    private bool _isEditable;

    private void UpdateLocalizedStrings()
    {
        WindowTitle = _isEditable ? _localization.Get("Monitoring.EditFormulaTitle") : _localization.Get("Monitoring.ViewFormulaTitle");
    }

    private void EditFormula_VM_ErrorsChanged(object? sender, System.ComponentModel.DataErrorsChangedEventArgs e)
    {
        SaveIsEnabled = ActualErrors.Count == 0;
    }

    public void InitWindow(string description, string? formula, bool isEnable)
    {
        _isEditable = isEnable;
        WindowTitle = isEnable ? _localization.Get("Monitoring.EditFormulaTitle") : _localization.Get("Monitoring.ViewFormulaTitle");
        Description = description;
        Formula = formula;
        UI_IsEnable = isEnable;
    }

    public void MarkAsSaved()
    {
        _saved = true;
    }

    public string? GetResult()
    {
        return _saved && !ActualErrors.Any() ? MathFormula.Normalize(Formula) : null;
    }

    #region Валидация

    protected override ValidateMessage? GetErrorMessage(string fieldName, string? value)
    {
        if (fieldName != nameof(Formula))
        {
            return null;
        }

        if (string.IsNullOrEmpty(value))
        {
            return new ValidateMessage(_localization.Get("Validation.EnterFormula"));
        }

        if (!MathFormula.IsValid(value, out string errorMessage))
        {
            return new ValidateMessage(errorMessage);
        }

        return null;
    }

    #endregion Валидация
}
