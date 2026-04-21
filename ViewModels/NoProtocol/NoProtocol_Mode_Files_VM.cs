using Core.Models.NoProtocol;
using Core.Models;
using Core.Models.Settings;
using MessageBox.Core;
using ReactiveUI;
using Services.Interfaces;
using System.Collections.ObjectModel;
using System.Reactive;
using Core.Clients.DataTypes;

namespace ViewModels.NoProtocol;

public class NoProtocol_Mode_Files_VM : ReactiveObject
{
    private bool _isSending = false;

    public bool IsSending
    {
        get => _isSending;
        set => this.RaiseAndSetIfChanged(ref _isSending, value);
    }

    private ObservableCollection<SendFileItem_VM> _files = new ObservableCollection<SendFileItem_VM>();

    public ObservableCollection<SendFileItem_VM> Files
    {
        get => _files;
        set => this.RaiseAndSetIfChanged(ref _files, value);
    }

    public ReactiveCommand<Unit, Unit> Command_AddFile { get; }
    public ReactiveCommand<Unit, Unit> Command_RemoveAllFiles { get; }

    private readonly IFileSystemService _fileSystemService;
    private readonly IMessageBoxMainWindow _messageBox;
    private readonly Model_Settings _settingsModel;
    private readonly ConnectedHost _connectedHostModel;
    private readonly Model_NoProtocol _noProtocolModel;

    private readonly ILocalizationService _localization;

    public NoProtocol_Mode_Files_VM(IFileSystemService fileSystemService, IMessageBoxMainWindow messageBox,
        Model_Settings settingsModel, ConnectedHost connectedHostModel, Model_NoProtocol noProtocolModel,
        ILocalizationService localization)
    {
        _fileSystemService = fileSystemService ?? throw new ArgumentNullException(nameof(fileSystemService));
        _messageBox = messageBox ?? throw new ArgumentNullException(nameof(messageBox));
        _settingsModel = settingsModel ?? throw new ArgumentNullException(nameof(settingsModel));
        _connectedHostModel = connectedHostModel ?? throw new ArgumentNullException(nameof(connectedHostModel));
        _noProtocolModel = noProtocolModel ?? throw new ArgumentNullException(nameof(noProtocolModel));
        _localization = localization ?? throw new ArgumentNullException(nameof(localization));

        _connectedHostModel.DeviceIsConnect += Model_DeviceIsConnect;
        _connectedHostModel.DeviceIsDisconnected += Model_DeviceIsDisconnected;

        Command_AddFile = ReactiveCommand.CreateFromTask(AddFileHandler);
        Command_AddFile.ThrownExceptions.Subscribe(error => _messageBox.Show(_localization.Get("Error.AddFile") + "\n\n" + error.Message, MessageType.Error, error));

        Command_RemoveAllFiles = ReactiveCommand.CreateFromTask(RemoveAllFiles);
        Command_RemoveAllFiles.ThrownExceptions.Subscribe(error => _messageBox.Show(_localization.Get("Error.RemoveAllFiles") + "\n\n" + error.Message, MessageType.Error, error));

        UpdateFileList();
    }

    private void Model_DeviceIsConnect(object? sender, IConnection? e)
    {
        foreach (var file in Files)
        {
            file.SendIsEnable = true;
        }
    }

    private void Model_DeviceIsDisconnected(object? sender, IConnection? e)
    {
        foreach (var file in Files)
        {
            file.SendIsEnable = false;
        }
    }

    private async Task AddFileHandler()
    {
        string? filePath = await _fileSystemService.GetFilePath(_localization.Get("NoProtocol.AddFileDialogTitle"), _localization.Get("NoProtocol.FileTypeLabel"), null);

        if (string.IsNullOrEmpty(filePath))
            return;

        string targetFilePath = _settingsModel.CopySendFileInWorkDirectory(filePath);

        var file = new SendFileItem_VM(Guid.NewGuid(), targetFilePath, SendFileHandler, RemoveFileHandler, _messageBox)
        {
            SendIsEnable = _connectedHostModel.HostIsConnect
        };

        Files.Add(file);
    }

    private async Task RemoveAllFiles()
    {
        if (Files.Count() == 0)
            return;

        if (await _messageBox.ShowYesNoDialog(_localization.Get("Confirm.DeleteAllFiles"), MessageType.Warning) == MessageBoxResult.Yes)
        {
            foreach (var file in Files)
            {
                _settingsModel.DeleteFile(file.FilePath);
            }

            Files.Clear();
        }
    }

    private async Task SendFileHandler(Guid id)
    {
        try
        {
            if (!_connectedHostModel.HostIsConnect)
                return;

            var file = Files.First(e => e.Id == id);

            if (file == null)
                return;

            var bytes = File.ReadAllBytes(file.FilePath);

            IsSending = true;

            await _noProtocolModel.SendBytes(bytes);

            IsSending = false;
        }
        
        catch (Exception error)
        {
            IsSending = false;

            throw new Exception(error.Message);
        }
    }

    private void RemoveFileHandler(Guid id)
    {
        var files = Files.First(e => e.Id == id);

        _settingsModel.DeleteFile(files.FilePath);

        Files.Remove(files);
    }

    private void UpdateFileList()
    {
        var files = _settingsModel.GetAllSendFilesNames();

        foreach (var filePath in files)
        {
            Files.Add(new SendFileItem_VM(Guid.NewGuid(), filePath, SendFileHandler, RemoveFileHandler, _messageBox));
        }
    }
}
