using MessageBox.Core;
using ReactiveUI;
using Services.Interfaces;
using System.Reactive;

namespace ViewModels.NoProtocol;

public class SendFileItem_VM : ReactiveObject
{
    private bool _sendIsEnable;

    public bool SendIsEnable
    {
        get => _sendIsEnable;
        set => this.RaiseAndSetIfChanged(ref _sendIsEnable, value);
    }

    private string? _fileName;

    public string? FileName
    {
        get => _fileName;
        set => this.RaiseAndSetIfChanged(ref _fileName, value);
    }

    private string? _fileExtension;

    public string? FileExtension
    {
        get => _fileExtension;
        set => this.RaiseAndSetIfChanged(ref _fileExtension, value);
    }

    private string? _fileSize;

    public string? FileSize
    {
        get => _fileSize;
        set => this.RaiseAndSetIfChanged(ref _fileSize, value);
    }

    public ReactiveCommand<Unit, Unit> Command_SendFile { get; }
    public ReactiveCommand<Unit, Unit> Command_RemoveFile { get; }

    public readonly Guid Id;
    public readonly string FilePath;

    public SendFileItem_VM(Guid id, string filePath, Func<Guid, Task> sendFileHandler, Action<Guid> removeFileHandler, IMessageBox messageBox)
    {
        Id = id;
        FilePath = filePath;

        FileName = Path.GetFileNameWithoutExtension(filePath);
        FileExtension = Path.GetExtension(filePath);
        FileSize = GetFileSize();

        Command_SendFile = ReactiveCommand.CreateFromTask(async () => await sendFileHandler(id));
        Command_SendFile.ThrownExceptions.Subscribe(error => messageBox.Show(LocalizationProvider.Get("Error.SendFile", FileName ?? string.Empty) + "\n\n" + error.Message, MessageType.Error, error));

        Command_RemoveFile = ReactiveCommand.CreateFromTask(async () =>
        {
            if (await messageBox.ShowYesNoDialog(LocalizationProvider.Get("Confirm.DeleteFile", FileName ?? string.Empty), MessageType.Warning) == MessageBoxResult.Yes)
            {
                removeFileHandler(id);
            }
        });
        Command_RemoveFile.ThrownExceptions.Subscribe(error => messageBox.Show(LocalizationProvider.Get("Error.RemoveFile", FileName ?? string.Empty) + "\n\n" + error.Message, MessageType.Error, error));
    }

    private string GetFileSize()
    {
        var fileInfo = new FileInfo(FilePath);

        var byteSize = fileInfo.Length;

        const long KB = 1024;
        const long MB = KB * 1024;
        const long GB = MB * 1024;
        const long TB = GB * 1024;

        if (byteSize < KB)
            return $"{byteSize} B";

        else if (byteSize < MB)
            return $"{(byteSize / (double)KB):F2} KB";

        else if (byteSize < GB)
            return $"{(byteSize / (double)MB):F2} MB";

        else if (byteSize < TB)
            return $"{(byteSize / (double)GB):F2} GB";

        else
            return $"{(byteSize / (double)TB):F2} TB";
    }
}
