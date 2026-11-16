using Core.Models.Settings.FileTypes;
using System.Globalization;
using System.Text;
using System.Threading.Channels;

namespace Core.Models.Logging;

public class FileLogger : IAsyncDisposable
{
    public event EventHandler<Exception>? WriteFailed;

    public bool IsRunning { get; private set; }

    private readonly object _syncObj = new();

    private Channel<LogEntry>? _channel;
    private Task? _writerTask;
    private StreamWriter? _writer;
    
    private CancellationTokenSource? _cts;
    
    private TimestampFormat _selectedTimestampFormat;
    
    private record LogEntry(DateTime Timestamp, string Data);

    private readonly ILocalizationService _localization;

    
    public FileLogger(ILocalizationService localization)
    {
        _localization = localization ?? throw new ArgumentNullException(nameof(localization));
    }

    public void Start(string filePath, string columnNames, TimestampFormat logTimestampFormat)
    {
        lock (_syncObj)
        {
            if (IsRunning || _writerTask != null)
                return;

            _selectedTimestampFormat = logTimestampFormat;

            StreamWriter? writer = null;

            try
            {
                var fileStream = new FileStream(
                    filePath,
                    FileMode.Create,
                    FileAccess.Write,
                    FileShare.Read,
                    bufferSize: 4096,
                    options: FileOptions.Asynchronous | FileOptions.WriteThrough);

                writer = new StreamWriter(fileStream, Encoding.UTF8)
                {
                    AutoFlush = true
                };

                writer.WriteLine(
                    $"{(_selectedTimestampFormat != TimestampFormat.None ? _localization.Get("Common.Time") + "\t" : string.Empty)}{columnNames}");

                _cts = new CancellationTokenSource();

                _channel = Channel.CreateUnbounded<LogEntry>(
                    new UnboundedChannelOptions
                    {
                        SingleReader = true,
                        SingleWriter = false
                    });

                _writer = writer;
                writer = null;

                _writerTask = Task.Run(() => WriteLoopAsync(_cts.Token), _cts.Token);
                IsRunning = true;
            }
            
            catch
            {
                writer?.Dispose();
                _writer?.Dispose();
                _cts?.Dispose();

                _channel = null;
                _writer = null;
                _writerTask = null;
                _cts = null;
                IsRunning = false;
                
                throw;
            }
        }
    }
    
    public void WriteLine(string data)
    {
        lock (_syncObj)
        {
            if (!IsRunning)
                return;

            _channel!.Writer.TryWrite(new LogEntry(DateTime.Now, data));
        }
    }

    public async Task StopAsync()
    {
        lock (_syncObj)
        {
            if (_writerTask == null)
                return;

            IsRunning = false;
            _cts?.Cancel();
        }

        _channel?.Writer.TryComplete();

        await _writerTask!;

        if (_writer != null)
        {
            try
            {
                await _writer.FlushAsync();
            }
            
            catch (Exception)
            {
                // Для перестраховки, как и сам FlushAsync()
            }

            await _writer.DisposeAsync();
        }

        _cts?.Dispose();

        lock (_syncObj)
        {
            _channel = null;
            _writer = null;
            _writerTask = null;
            _cts = null;
        }
    }

    private async Task WriteLoopAsync(CancellationToken token)
    {
        try
        {
            await foreach (var entry in _channel!.Reader.ReadAllAsync(token))
            {
                await _writer!.WriteLineAsync(
                    $"{GetTimestamp(entry)}{entry.Data}");
            }
        }
        
        catch (OperationCanceledException)
        {
            
        }
        
        catch (Exception error)
        {
            lock (_syncObj)
            {
                IsRunning = false;
                _channel?.Writer.TryComplete();
            }

            WriteFailed?.Invoke(this, error);
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

            case TimestampFormat.ISO8601:
                return $"{entry.Timestamp.ToString("o", CultureInfo.InvariantCulture)}\t";

            default:
                return $"{entry.Timestamp.ToString("dd.MM.yyyy HH:mm:ss:fff", CultureInfo.InvariantCulture)}\t";
        }
    }

    public async ValueTask DisposeAsync()
    {
        await StopAsync();
    }
}
