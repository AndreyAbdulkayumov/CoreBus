using Avalonia.Controls;
using Avalonia.Platform.Storage;
using CoreBus.Base.Views;
using Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace CoreBus.Base.Services;

public class FileSystemService : IFileSystemService
{
    public async Task<string?> GetFilePath(string windowTitle, string pickerFileTypeName, IReadOnlyList<string>? patterns)
    {
        // Get top level from the current control. Alternatively, you can use Window reference instead.
        TopLevel? topLevel = TopLevel.GetTopLevel(MainWindow.Instance);

        if (topLevel != null)
        {
            var options = new FilePickerOpenOptions
            {
                Title = windowTitle,
                AllowMultiple = false
            };

            if (patterns != null)
            {
                options.FileTypeFilter = [new FilePickerFileType(pickerFileTypeName) { Patterns = patterns }];
            }

            // Start async operation to open the dialog.
            var files = await topLevel.StorageProvider.OpenFilePickerAsync(options);

            if (files.Count >= 1)
            {
                return files.First().TryGetLocalPath();
            }
        }

        return null;
    }

    public async Task<string?> GetFolderPath(string windowTitle)
    {
        // Get top level from the current control. Alternatively, you can use Window reference instead.
        TopLevel? topLevel = TopLevel.GetTopLevel(MainWindow.Instance);

        if (topLevel != null)
        {
            var folder = await topLevel.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
            {
                Title = windowTitle,
                AllowMultiple = false
            });

            if (folder != null && folder.Count > 0)
            {
                return folder.First().TryGetLocalPath();
            }
        }

        return null;
    }

    public void OpenUserManual()
    {
        if (OperatingSystem.IsWindows())
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = Path.Combine("Documentation", "UserManual.pdf"),
                UseShellExecute = true,
            });

            return;
        }

        else if (OperatingSystem.IsLinux())
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "xdg-open",
                Arguments = Path.Combine("Documentation", "UserManual.pdf"),
                UseShellExecute = false,
            });

            return;
        }

        throw new Exception("Неподдерживаемый тип ОС.");
    }

    public async Task OpenFolder(string folderPath)
    {
        var topLevel = TopLevel.GetTopLevel(MainWindow.Instance);

        var dirInfo = new DirectoryInfo(folderPath);

        if (Directory.Exists(folderPath) && topLevel != null)
        {
            bool successOpen = await topLevel.Launcher.LaunchDirectoryInfoAsync(dirInfo);

            if (!successOpen)
                throw new Exception("Не удалось открыть папку с логами");
        }
    }
}
