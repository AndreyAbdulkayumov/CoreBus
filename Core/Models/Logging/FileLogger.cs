using Core.Models.Settings.FileTypes;
using System.Globalization;
using System.Text;
using System.Threading.Channels;

namespace Core.Models.Logging;

public class FileLogger : IAsyncDisposable
{
    private Channel<LogEntry>? _channel;
    private Task? _writerTask;
    private StreamWriter? _writer;
    private CancellationTokenSource? _cts;

    private record LogEntry(DateTime Timestamp, string Data);

    public bool IsRunning => _channel != null;

    private TimestampFormat _selectedTimestampFormat;

    public void Start(string filePath, string columnNames, TimestampFormat logTimestampFormat)
    {
        if (IsRunning)
            return;

        _selectedTimestampFormat = logTimestampFormat;

        _cts = new CancellationTokenSource();

        _channel = Channel.CreateUnbounded<LogEntry>(
            new UnboundedChannelOptions
            {
                SingleReader = true,
                SingleWriter = false
            });

        var fileStream = new FileStream(
            filePath,
            FileMode.Create,
            FileAccess.Write,
            FileShare.Read,
            bufferSize: 4096,
            options: FileOptions.Asynchronous | FileOptions.WriteThrough);

        _writer = new StreamWriter(fileStream, Encoding.UTF8)
        {
            AutoFlush = true
        };

        _writer.WriteLine($"{(_selectedTimestampFormat != TimestampFormat.None ? "Время\t" : string.Empty)}{columnNames}");

        _writerTask = Task.Run(() => WriteLoopAsync(_cts.Token));
    }

    public void WriteLine(string data)
    {
        if (!IsRunning)
            return;

        _channel!.Writer.TryWrite(new LogEntry(DateTime.Now, data));
    }

    public async Task StopAsync()
    {
        if (!IsRunning)
            return;

        _channel!.Writer.Complete();

        if (_writerTask != null)
            await _writerTask;

        await _writer!.FlushAsync();
        await _writer.DisposeAsync();

        _cts!.Dispose();

        _channel = null;
        _writer = null;
        _writerTask = null;
        _cts = null;
    }

    private async Task WriteLoopAsync(CancellationToken token)
    {
        await foreach (var entry in _channel!.Reader.ReadAllAsync(token))
        {
            await _writer!.WriteLineAsync(
                $"{GetTimestamp(entry)}{entry.Data}");
        }
    }

    private string GetTimestamp(LogEntry entry)
    {
        switch (_selectedTimestampFormat)
        {
            case TimestampFormat.None:
                return string.Empty;

            case TimestampFormat.Time:
                return $"{entry.Timestamp.ToString("HH:mm:ss:fff", CultureInfo.InvariantCulture)}\t";

            case TimestampFormat.DateTime:
                return $"{entry.Timestamp.ToString("dd.MM.yyyy HH:mm:ss:fff", CultureInfo.InvariantCulture)}\t";

            default:
                return $"{entry.Timestamp.ToString("dd.MM.yyyy HH:mm:ss:fff", CultureInfo.InvariantCulture)}\t";
        }
    }

    public async ValueTask DisposeAsync()
    {
        await StopAsync();
    }
}
