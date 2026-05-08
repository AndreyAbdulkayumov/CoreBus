using System.Net.Sockets;
using Core.Clients.DataTypes;
using Core.Models;
using Services.Interfaces;

namespace Core.Clients;

public class IPClient : IConnection
{
    public event EventHandler<byte[]>? DataReceived;
    public event EventHandler<Exception>? ErrorInReadThread;

    public bool IsConnected { get; private set; } = false;

    public int WriteTimeout
    {
        get
        {
            if (_stream != null)
            {
                return _stream.WriteTimeout;
            }

            return 0;
        }

        set
        {
            if (_stream != null)
            {
                _stream.WriteTimeout = value;
            }
        }
    }

    public int ReadTimeout
    {
        get
        {
            if (_stream != null)
            {
                return _stream.ReadTimeout;
            }

            return 0;
        }

        set
        {
            if (_stream != null)
            {
                _stream.ReadTimeout = value;
            }
        }
    }

    public NotificationSource Notifications { get; private set; }

    private NetworkStream? _stream;
    private TcpClient? _client;

    private Task? _readThread;
    private CancellationTokenSource? _readCancelSource;
    
    private readonly ILocalizationService _localization;


    public IPClient(ILocalizationService localization)
    {
        _localization = localization ?? throw new ArgumentNullException(nameof(localization));
        
        Notifications = new NotificationSource(
            TX_ViewLatency_ms: 100,
            RX_ViewLatency_ms: 100,
            CheckInterval_ms: 10
            );
    }

    public void SetReadMode(ReadMode mode)
    {
        switch (mode)
        {
            case ReadMode.Async:

                if (_stream != null && IsConnected)
                {
                    _readCancelSource = new CancellationTokenSource();

                    _readThread = Task.Run(() => AsyncThread_Read(_stream, _readCancelSource.Token));
                }

                break;

            case ReadMode.Sync:

                if (_stream != null && IsConnected)
                {
                    _readCancelSource?.Cancel();

                    if (_readThread != null)
                    {
                        Task waitCancel = Task.WhenAll(_readThread);

                        Task flushTask = _stream.FlushAsync();

                        Task.WaitAll(waitCancel, flushTask);
                    }
                }

                break;

            default:
                throw new Exception(_localization.Get("Core.UnknownReadMode", mode.ToString()));
        }
    }

    public void Connect(ConnectionInfo information)
    {
        var socketInfo = information.Info as SocketInfo;

        if (socketInfo == null)
        {
            throw new Exception(_localization.Get("Core.EthernetSettingsMissing"));
        }

        if (string.IsNullOrEmpty(socketInfo.IP) ||
            string.IsNullOrEmpty(socketInfo.Port))
        {
            throw new Exception(
                (string.IsNullOrEmpty(socketInfo.IP) ? _localization.Get("ConnectionInfo.IpAddressNotSet") + "\n" : "") +
                (string.IsNullOrEmpty(socketInfo.Port) ? _localization.Get("ConnectionInfo.PortNotSet") : "")
                );
        }

        if (int.TryParse(socketInfo.Port, out int Port) == false)
        {
            throw new Exception(_localization.Get("Core.PortParseError", socketInfo.Port));
        }

        _client = new TcpClient();

        IAsyncResult result = _client.BeginConnect(socketInfo.IP, Port, null, null);

        if (result.AsyncWaitHandle.WaitOne(500, true) == true)
        {
            _client.EndConnect(result);
        }

        else
        {
            _client.Close();

            throw new Exception(_localization.Get("Core.ServerConnectError", socketInfo.IP, socketInfo.Port));
        }

        _stream = _client.GetStream();

        Notifications.StartMonitor();

        IsConnected = true;
    }

    public async Task Disconnect()
    {
        ProtocolMode? selectedProtocol = ConnectedHost.SelectedProtocol;

        if (selectedProtocol != null && selectedProtocol.CurrentReadMode == ReadMode.Async)
        {
            _readCancelSource?.Cancel();

            if (_readThread != null)
            {
                await Task.WhenAll(_readThread).ConfigureAwait(false);
            }
        }

        _stream?.Close();

        _client?.Close();

        await Notifications.StopMonitor();

        IsConnected = false;
    }

    public async Task<ModbusOperationInfo> Send(byte[] message, int numberOfBytes)
    {
        if (_stream == null)
        {
            return new ModbusOperationInfo(DateTime.Now, null);
        }

        var executionTime = new DateTime();

        try
        {
            if (IsConnected)
            {
                await _stream.WriteAsync(message, 0, numberOfBytes);

                executionTime = DateTime.Now;

                Notifications.TransmitEvent();
            }

            return new ModbusOperationInfo(executionTime, null);
        }

        catch (Exception error)
        {
            throw new Exception(_localization.Get("Message.Error.SendData") + "\n\n" + error.Message + "\n\n" +
                _localization.Get("Core.WriteTimeoutPrefix") +
                (_stream.WriteTimeout == Timeout.Infinite ?
                _localization.Get("Core.TimeoutInfinite") : _stream.WriteTimeout.ToString() + " " + _localization.Get("Common.Ms")));
        }
    }

    public async Task<ModbusOperationInfo> Receive()
    {
        if (_stream == null)
        {
            return new ModbusOperationInfo(DateTime.Now, Array.Empty<byte>());
        }

        var receivedBytes = new List<byte>();

        DateTime executionTime = new DateTime();

        try
        {
            if (IsConnected)
            {
                byte[] buffer;

                int numberOfReceivedBytes;

                bool isFirstPackage = true;

                do
                {
                    buffer = new byte[100];

                    // Асинхронная операция не среагирует на срабатывание таймаута чтения.
                    // Поэтому чтобы предотвратить зависание программы на этом моменте, заведен токен отмены.
                    var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(_stream.ReadTimeout));

                    try
                    {
                        numberOfReceivedBytes = await _stream.ReadAsync(buffer, 0, buffer.Length, cts.Token);
                    }

                    catch (OperationCanceledException)
                    {
                        throw new Exception(_localization.Get("Core.HostNoResponseInTimeout"));
                    }

                    if (isFirstPackage)
                    {
                        executionTime = DateTime.Now;
                        isFirstPackage = false;
                    }

                    Array.Resize(ref buffer, numberOfReceivedBytes);

                    receivedBytes.AddRange(buffer);

                } while (_stream.DataAvailable);

                executionTime = DateTime.Now;

                if (receivedBytes.Count > 0)
                {
                    Notifications.ReceiveEvent();
                }
            }

            return new ModbusOperationInfo(executionTime, receivedBytes.ToArray());
        }

        catch (Exception error)
        {
            throw new Exception(_localization.Get("Core.ReceiveDataError") + "\n\n" + error.Message + "\n\n" +
                _localization.Get("Core.ReadTimeoutPrefix") +
                (_stream.ReadTimeout == Timeout.Infinite ?
                _localization.Get("Core.TimeoutInfinite") : _stream.ReadTimeout.ToString() + " " + _localization.Get("Common.Ms")));
        }
    }

    private async Task AsyncThread_Read(NetworkStream? currentStream, CancellationToken readCancel)
    {
        try
        {
            if (currentStream == null)
                throw new InvalidOperationException(_localization.Get("Core.ReadStreamNotInitialized"));

            byte[] bufferRX = new byte[currentStream.Socket.ReceiveBufferSize];

            int numberOfReceiveBytes;

            Task<int> readResult;

            var waitCancel = Task.Delay(Timeout.Infinite, readCancel);

            Task completedTask;

            while (true)
            {
                readCancel.ThrowIfCancellationRequested();

                /// Метод асинхронного чтения у объекта класса NetworkStream
                /// почему то не обрабатывает событие отмены у токена отмены.
                /// Судя по формумам это происходит из - за того что внутри метода происходят 
                /// неуправляемые вызовы никоуровневого API (в MSDN об этом не сказано).
                /// Поэтому для отслеживания состояния токена отмены была создана задача WaitCancel.

                readResult = currentStream.ReadAsync(bufferRX, 0, bufferRX.Length, readCancel);

                completedTask = await Task.WhenAny(readResult, waitCancel).ConfigureAwait(false);

                if (completedTask == waitCancel)
                    throw new OperationCanceledException();

                numberOfReceiveBytes = await readResult;

                DataReceived?.Invoke(this, bufferRX.Take(numberOfReceiveBytes).ToArray());

                Notifications.ReceiveEvent();

                Array.Clear(bufferRX, 0, numberOfReceiveBytes);
            }
        }

        catch (OperationCanceledException)
        {
            //  Возникает при отмене задачи.
            //  По правилам отмены асинхронных задач это исключение можно игнорировать.
        }

        catch (Exception error)
        {
            ErrorInReadThread?.Invoke(this, error);
        }
    }
}