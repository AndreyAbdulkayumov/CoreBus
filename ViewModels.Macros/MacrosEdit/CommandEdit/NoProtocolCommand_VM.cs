using ReactiveUI;
using System.Collections.ObjectModel;
using Core.Models.Settings.FileTypes;
using ViewModels.Helpers;
using ViewModels.Macros.DataTypes;

namespace ViewModels.Macros.MacrosEdit.CommandEdit;

public class NoProtocolCommand_VM : ReactiveObject, ICommandContent, ICommandValidation
{
    private readonly Guid _id;

    public Guid Id => _id;

    private string? _name = string.Empty;

    public string? Name
    {
        get => _name;
        set => this.RaiseAndSetIfChanged(ref _name, value);
    }

    private readonly ObservableCollection<string> _typeOfEncoding = new ObservableCollection<string>()
    {
        AppEncoding.Name_ASCII, AppEncoding.Name_UTF8, AppEncoding.Name_UTF32, AppEncoding.Name_Unicode
    };

    public ObservableCollection<string> TypeOfEncoding
    {
        get => _typeOfEncoding;
    }

    private string? _selectedEncoding = AppEncoding.Name_UTF8;

    public string? SelectedEncoding
    {
        get => _selectedEncoding;
        set => this.RaiseAndSetIfChanged(ref _selectedEncoding, value);
    }

    private string? _messageString;

    public string? MessageString
    {
        get => _messageString;
        set => this.RaiseAndSetIfChanged(ref _messageString, value);
    }

    private bool _cr_enable;

    public bool CR_Enable
    {
        get => _cr_enable;
        set => this.RaiseAndSetIfChanged(ref _cr_enable, value);
    }

    private bool _lf_enable;

    public bool LF_Enable
    {
        get => _lf_enable;
        set => this.RaiseAndSetIfChanged(ref _lf_enable, value);
    }

    private bool _isBytesSend;

    public bool IsBytesSend
    {
        get => _isBytesSend;
        set => this.RaiseAndSetIfChanged(ref _isBytesSend, value);
    }

    private bool _isInit;

    public NoProtocolCommand_VM(Guid id, EditCommandParameters parameters)
    {
        _isInit = false;

        _id = id;

        Name = parameters.CommandName;

        if (parameters.InitData is MacrosCommandNoProtocol data && data.Content != null)
        {
            SelectedEncoding = string.IsNullOrEmpty(data.Content.MacrosEncoding) ? AppEncoding.Name_UTF8 : data.Content.MacrosEncoding;
            MessageString = data.Content.Message;
            IsBytesSend = data.Content.IsByteString;
            CR_Enable = data.Content.EnableCR;
            LF_Enable = data.Content.EnableLF;
        }

        this.WhenAnyValue(x => x.IsBytesSend)
            .Subscribe(IsBytes =>
            {
                if (_isInit && !string.IsNullOrEmpty(MessageString))
                {
                    MessageString = StringByteConverter.GetMessageString(MessageString, IsBytes, AppEncoding.GetEncoding(SelectedEncoding));
                }
            });

        _isInit = true;
    }

    public object GetContent()
    {
        return new MacrosCommandNoProtocol()
        {
            Name = Name,
            Content = new NoProtocolCommandContent()
            {
                MacrosEncoding = SelectedEncoding,
                Message = MessageString,
                IsByteString = IsBytesSend,
                EnableCR = CR_Enable,
                EnableLF = LF_Enable,
            }
        };
    }

    public string GetValidatedString()
    {
        if (string.IsNullOrEmpty(MessageString))
        {
            return string.Empty;
        }

        if (IsBytesSend)
        {
            return StringByteConverter.GetValidatedByteString(MessageString);
        }

        return MessageString;
    }

    public string? GetValidationMessage(params FieldNames[] uncheckedFields)
    {
        if (string.IsNullOrEmpty(MessageString))
        {
            return "Введите данные для отправки.";
        }

        return null;
    }
}
