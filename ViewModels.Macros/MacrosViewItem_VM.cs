using ReactiveUI;
using Services.Interfaces;
using System.Reactive;
using MessageBox.Core;

namespace ViewModels.Macros;

public class MacrosViewItem_VM : ReactiveObject
{
    private string _title = string.Empty;

    public string Title
    {
        get => _title;
        set => this.RaiseAndSetIfChanged(ref _title, value);
    }

    public Action ClickAction;

    public ReactiveCommand<Unit, Unit> Command_EditMacros { get; }
    public ReactiveCommand<Unit, Unit> Command_MacrosDelete { get; }

    private readonly IMessageBox _messageBox;

    public MacrosViewItem_VM(string title, Action clickAction, Func<string, Task> editMacros, Action<string> deleteMacrosAction, IMessageBox messageBox)
    {
        Title = title;

        ClickAction = clickAction;

        _messageBox = messageBox;

        Command_EditMacros = ReactiveCommand.CreateFromTask(() => editMacros(Title));
        Command_EditMacros.ThrownExceptions.Subscribe(error => messageBox.Show(LocalizationProvider.Get("Error.EditMacro", Title) + "\n\n" + error.Message, MessageType.Error, error));

        Command_MacrosDelete = ReactiveCommand.CreateFromTask(() => DeleteMacros(deleteMacrosAction));
        Command_MacrosDelete.ThrownExceptions.Subscribe(error => messageBox.Show(LocalizationProvider.Get("Error.RemoveMacro", Title) + "\n\n" + error.Message, MessageType.Error, error));
    }

    public void MacrosAction()
    {
        try
        {
            ClickAction();
        }

        catch (Exception error)
        {
            _messageBox.Show(error.Message, MessageType.Error, error);
        }
    }

    private async Task DeleteMacros(Action<string> deleteMacrosAction)
    {
        if (await _messageBox.ShowYesNoDialog(LocalizationProvider.Get("Confirm.DeleteMacro", Title), MessageType.Information) == MessageBoxResult.Yes)
        {
            deleteMacrosAction(Title);
        }
    }
}
