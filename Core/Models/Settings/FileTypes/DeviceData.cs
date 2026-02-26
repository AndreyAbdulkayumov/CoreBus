namespace Core.Models.Settings.FileTypes;

public class SerialPort_Info
{
    public string? Port { get; set; }
    public string? BaudRate { get; set; }
    public bool BaudRate_IsCustom { get; set; }
    public string? BaudRate_Custom { get; set; }
    public string? Parity { get; set; }
    public string? DataBits { get; set; }
    public string? StopBits { get; set; }
}

public class IP_Info
{
    public string? IP_Address { get; set; }
    public string? Port { get; set; }
}

// Вариант по умолчанию должен быть первым
public enum TimestampFormat
{
    DateTime,
    None,
    Time
}

public class DeviceData : ICloneable
{
    public const string ConnectionName_SerialPort = "SerialPort";
    public const string ConnectionName_Ethernet = "Ethernet";

    public const string FloatWriteFormat_AB_CD = "AB_CD";
    public const string FloatWriteFormat_BA_DC = "BA_DC";
    public const string FloatWriteFormat_CD_AB = "CD_AB";
    public const string FloatWriteFormat_DC_BA = "DC_BA";

    // Настройки подключения
    public string? TypeOfConnection { get; set; }
    public SerialPort_Info? Connection_SerialPort { get; set; }
    public IP_Info? Connection_IP { get; set; }

    public const string TypeOfConnection_Default = ConnectionName_SerialPort;

    // Настройки режима "Без протокола"
    public string? GlobalEncoding { get; set; }

    public const string GlobalEncoding_Default = "UTF-8";

    // Настройки режима "Modbus"
    public string? TimeoutWrite { get; set; }
    public string? TimeoutRead { get; set; }
    public TimestampFormat LogTimestampFormat { get; set; }
    public string? FloatNumberFormat { get; set; }

    public const string TimeoutWrite_Default = "300";
    public const string TimeoutRead_Default = "300";
    public const string FloatNumberFormat_Default = FloatWriteFormat_BA_DC;
    public const TimestampFormat LogTimestampFormat_Default = TimestampFormat.DateTime;

    public static DeviceData GetDefault()
    {
        return new DeviceData()
        {
            TypeOfConnection = TypeOfConnection_Default,
            Connection_SerialPort = null,
            Connection_IP = null,

            GlobalEncoding = GlobalEncoding_Default,

            TimeoutWrite = TimeoutWrite_Default,
            TimeoutRead = TimeoutRead_Default,
            LogTimestampFormat = LogTimestampFormat_Default,
            FloatNumberFormat = FloatNumberFormat_Default,
        };
    }

    public object Clone()
    {
        SerialPort_Info? SerialPort;

        if (Connection_SerialPort != null)
        {
            SerialPort = new SerialPort_Info()
            {
                Port = Connection_SerialPort.Port,
                BaudRate = Connection_SerialPort.BaudRate,
                BaudRate_IsCustom = Connection_SerialPort.BaudRate_IsCustom,
                BaudRate_Custom = Connection_SerialPort.BaudRate_Custom,
                Parity = Connection_SerialPort.Parity,
                DataBits = Connection_SerialPort.DataBits,
                StopBits = Connection_SerialPort.StopBits
            };
        }

        else
        {
            SerialPort = null;
        }

        IP_Info? IP;

        if (Connection_IP != null)
        {
            IP = new IP_Info()
            {
                IP_Address = Connection_IP.IP_Address,
                Port = Connection_IP.Port
            };
        }

        else
        {
            IP = null;
        }

        return new DeviceData()
        {
            TypeOfConnection = TypeOfConnection,
            Connection_SerialPort = SerialPort,
            Connection_IP = IP,

            GlobalEncoding = GlobalEncoding,

            TimeoutWrite = TimeoutWrite,
            TimeoutRead = TimeoutRead,
            LogTimestampFormat = LogTimestampFormat,
            FloatNumberFormat = FloatNumberFormat,
        };
    }
}
