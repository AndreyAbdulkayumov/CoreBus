using ReactiveUI.Reactive;
using System.Collections;
using System.ComponentModel;

namespace ViewModels.Validation;

public class ValidatedDateInputBase : ReactiveObject, INotifyDataErrorInfo
{
    public event EventHandler<DataErrorsChangedEventArgs>? ErrorsChanged;

    private bool _hasErrors;

    public bool HasErrors
    {
        set => this.RaiseAndSetIfChanged(ref _hasErrors, value);
        get => _hasErrors;
    }

    protected readonly Dictionary<string, ValidateMessage> _errors = new();

    protected void OnErrorsChanged(string propertyName)
    {
        ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(propertyName));
    }

    public IEnumerable GetErrors(string? propertyName)
    {
        HasErrors = _errors.Count != 0;

        if (!string.IsNullOrEmpty(propertyName) && _errors.TryGetValue(propertyName, out var validationMessage))
        {
            return new string[] { validationMessage.Short };
        }

        return Enumerable.Empty<string>();
    }
}
