using Core.Models;
using ReactiveUI;
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


    public EditFormula_VM()
    {
        ErrorsChanged += EditFormula_VM_ErrorsChanged;
    }

    private void EditFormula_VM_ErrorsChanged(object? sender, System.ComponentModel.DataErrorsChangedEventArgs e)
    {
        SaveIsEnabled = ActualErrors.Count == 0;
    }

    public void InitWindow(string description, string? formula, bool isEnable)
    {
        WindowTitle = isEnable ? "Редактирование формулы" : "Просмотр формулы";
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
            return new ValidateMessage("Введите формулу");
        }

        if (!MathFormula.IsValid(value, out string errorMessage))
        {
            return new ValidateMessage(errorMessage);
        }

        return null;
    }

    #endregion Валидация
}
