using Core.Models;
using ReactiveUI;
using ViewModels.Validation;

namespace ViewModels.ModbusClient.Monitoring;

public class EditFormula_VM : ValidatedDateInput
{
    private string? _title;

    public string? Title
    {
        get => _title;
        set => this.RaiseAndSetIfChanged(ref _title, value);
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

    public void InitWindow(string title, string? formula)
    {
        Title = title;
        Formula = formula;
    }

    public void MarkAsSaved()
    {
        _saved = true;
    }

    public string? GetResult()
    {
        return _saved && !ActualErrors.Any() ? MathFormula.Normalize(Formula) : null;
    }

    protected override ValidateMessage? GetErrorMessage(string fieldName, string? value)
    {
        if (fieldName != nameof(Formula))
        {
            return null;
        }

        if (string.IsNullOrEmpty(value))
        {
            return new ValidateMessage("Введите формулу", "Поле не может быть пустым");
        }


        if (!MathFormula.IsValid(value, out string errorMessage))
        {
            return new ValidateMessage(errorMessage, errorMessage);
        }

        return null;
    }
}
