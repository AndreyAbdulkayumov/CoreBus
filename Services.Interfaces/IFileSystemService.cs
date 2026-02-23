namespace Services.Interfaces;

public interface IFileSystemService
{
    Task<string?> GetFilePath(string windowTitle, string pickerFileType, IReadOnlyList<string>? patterns);
    Task<string?> GetFolderPath(string windowTitle);
    void OpenUserManual();
    Task OpenFolder(string folderPath);
}
