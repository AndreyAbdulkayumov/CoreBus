using ReactiveUI;
using Services.Interfaces;
using System.Reactive;
using MessageBox.Core;
using ViewModels.Macros.DataTypes;

namespace ViewModels.Macros.MacrosEdit;

public class MacrosCommandItem_VM : ReactiveObject
{
    private string? _commandName = string.Empty;

    public string? CommandName
    {
        get => _commandName;
        set => this.RaiseAndSetIfChanged(ref _commandName, value);
    }

    private bool _isEdit;

    public bool IsEdit
    {
        get => _isEdit;
        set => this.RaiseAndSetIfChanged(ref _isEdit, value);
    }

    public ReactiveCommand<Unit, Unit> Command_RunCommand { get; }
    public ReactiveCommand<Unit, Unit> Command_EditCommand { get; }
    public ReactiveCommand<Unit, Unit> Command_RemoveCommand { get; }

    public readonly Guid Id;

    public MacrosCommandItem_VM(Guid id, EditCommandParameters parameters, Action<Guid> runCommandHandler, Action<Guid> editCommandHandler, Action<Guid> removeItemHandler, IMessageBox messageBox)
    {
        Id = id;

        CommandName = parameters.CommandName;

        Command_RunCommand = ReactiveCommand.Create(() => runCommandHandler(id));
        Command_RunCommand.ThrownExceptions.Subscribe(error => messageBox.Show(LocalizationProvider.Get("Error.RunCommand", CommandName ?? string.Empty) + "\n\n" + error.Message, MessageType.Error, error));

        Command_EditCommand = ReactiveCommand.Create(() => editCommandHandler(Id));
        Command_EditCommand.ThrownExceptions.Subscribe(error => messageBox.Show(LocalizationProvider.Get("Error.EditCommand", CommandName ?? string.Empty) + "\n\n" + error.Message, MessageType.Error, error));

        Command_RemoveCommand = ReactiveCommand.CreateFromTask(async () =>
        {
            if (await messageBox.ShowYesNoDialog(LocalizationProvider.Get("Confirm.DeleteCommand", CommandName ?? string.Empty), MessageType.Warning) == MessageBoxResult.Yes)
            {
                removeItemHandler(Id);
            }
        });
        Command_RemoveCommand.ThrownExceptions.Subscribe(error => messageBox.Show(LocalizationProvider.Get("Error.RemoveCommand", CommandName ?? string.Empty) + "\n\n" + error.Message, MessageType.Error, error));
    }
}
