using Core.Clients.DataTypes;
using Core.Models.NoProtocol.DataTypes;
using Services.Interfaces;
using System.Text;

namespace Core.Models.NoProtocol;

public class Model_NoProtocol
{
    public double CycleMode_Period
    {
        get => CycleModeTimer.Interval;
        set => CycleModeTimer.Interval = value;
    }

    public event EventHandler<NoProtocolDataReceivedEventArgs>? Model_DataReceived;
    public event EventHandler<Exception>? Model_ErrorInReadThread;
    public event EventHandler<Exception>? Model_ErrorInCycleMode;

    public Encoding HostEncoding => ConnectedHost.GlobalEncoding;

    private IConnection? _client;

    private readonly System.Timers.Timer CycleModeTimer;
    private const double IntervalDefault = 100;

    private CycleModeParameters? _cycleModeInfo;

    private string[]? _outputArray;
    private int _resultIndex;

    private DateTime _outputDateTime;

    private bool _dateTime_IsUsed = false;

    private bool _date_IsUsed = false;

    private bool _time_IsUsed = false;
    private int _timeIndex;
    
    private readonly ILocalizationService _localization;


    public Model_NoProtocol(ILocalizationService localization)
    {
        _localization = localization ?? throw new ArgumentNullException(nameof(localization));
        
        CycleModeTimer = new System.Timers.Timer(IntervalDefault);
        CycleModeTimer.Elapsed += CycleModeTimer_Elapsed;
    }

    public void Host_DeviceIsConnect(object? sender, IConnection? e)
    {
        if (e != null && e.IsConnected)
        {
            _client = e;

            _client.DataReceived += Client_DataReceived;
            _client.ErrorInReadThread += Client_ErrorInReadThread;
        }
    }

    public void Host_DeviceIsDisconnected(object? sender, IConnection? e)
    {
        _client = null;
    }

    private void Client_DataReceived(object? sender, byte[] e)
    {
        if (_outputArray != null)
        {
            if (_dateTime_IsUsed)
            {
                _outputDateTime = DateTime.Now;

                if (_date_IsUsed)
                {
                    _outputArray[0] = _outputDateTime.ToShortDateString();
                }

                if (_time_IsUsed)
                {
                    _outputArray[_timeIndex] = _outputDateTime.ToLongTimeString();
                }
            }

            Model_DataReceived?.Invoke(this, new NoProtocolDataReceivedEventArgs(e, _outputArray, _resultIndex));

            return;
        }

        Model_DataReceived?.Invoke(this, new NoProtocolDataReceivedEventArgs(e));
    }

    private void Client_ErrorInReadThread(object? sender, Exception e)
    {
        CycleMode_Stop();

        Model_ErrorInReadThread?.Invoke(this, e);
    }

    public async Task SendBytes(byte[]? bytes)
    {
        if (_client == null)
        {
            throw new Exception(_localization.Get("Core.ClientNotInitialized"));
        }

        if (bytes == null || bytes.Length == 0)
        {
            throw new Exception(_localization.Get("Core.SendBufferEmpty"));
        }

        await _client.Send(bytes, bytes.Length);
    }

    public async Task CycleMode_Start(CycleModeParameters info)
    {
        _cycleModeInfo = info;

        _outputArray = CreateOutputBuffer(info);

        await SendBytes(info.MessageBytes);

        CycleModeTimer.Start();
    }

    private string[] CreateOutputBuffer(CycleModeParameters info)
    {
        var RX = new List<string>();

        _resultIndex = 0;

        _date_IsUsed = false;

        _time_IsUsed = false;
        _timeIndex = 0;

        if (info.Response_Date_Enable)
        {
            RX.Add(String.Empty);   // Элемент для даты

            _resultIndex++;

            _dateTime_IsUsed = true;

            _date_IsUsed = true;

            _timeIndex++;
        }

        if (info.Response_Time_Enable)
        {
            RX.Add(String.Empty);   // Элемент для времени

            _resultIndex++;

            _dateTime_IsUsed = true;

            _time_IsUsed = true;
        }

        if (info.Response_String_Start_Enable)
        {
            // Элемент для пользовательской строки в начале
            RX.Add(info.Response_String_Start == null ? String.Empty : info.Response_String_Start);
            _resultIndex++;
        }

        // Элемент для принимаемых данных
        RX.Add(String.Empty);

        if (info.Response_String_End_Enable)
        {
            // Элемент для пользовательской строки в конце
            RX.Add(info.Response_String_End == null ? String.Empty : info.Response_String_End);
        }

        // Два элемента для специальных символов

        if (info.Response_NextLine_Enable)
        {
            RX.Add(Environment.NewLine);
        }

        return RX.ToArray();
    }

    public void CycleMode_Stop()
    {
        CycleModeTimer.Stop();

        _outputArray = null;
    }

    private async void CycleModeTimer_Elapsed(object? sender, System.Timers.ElapsedEventArgs e)
    {
        try
        {
            if (_cycleModeInfo == null)
            {
                throw new Exception(_localization.Get("Common.NoDataToSend"));
            }

            await SendBytes(_cycleModeInfo.MessageBytes);
        }

        catch (Exception error)
        {
            CycleMode_Stop();

            Model_ErrorInCycleMode?.Invoke(this, error);
        }
    }
}
