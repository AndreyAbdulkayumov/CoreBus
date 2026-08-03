using ReactiveUI.Reactive;
using System.Collections.ObjectModel;
using ViewModels.Helpers;

namespace ViewModels.Settings.Tabs;

public class Settings_NoProtocol_VM : ReactiveObject
{
    private readonly ObservableCollection<string> _typeOfEncoding = new ObservableCollection<string>()
    {
        AppEncoding.Name_ASCII, AppEncoding.Name_UTF8, AppEncoding.Name_UTF32, AppEncoding.Name_Unicode
    };

    public ObservableCollection<string> TypeOfEncoding
    {
        get => _typeOfEncoding;
    }

    private string _selectedEncoding = string.Empty;

    public string SelectedEncoding
    {
        get => _selectedEncoding;
        set => this.RaiseAndSetIfChanged(ref _selectedEncoding, value);
    }

    public Settings_NoProtocol_VM()
    {

    }
}
