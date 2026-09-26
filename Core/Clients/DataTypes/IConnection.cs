namespace Core.Clients.DataTypes;

public interface IConnection
{
    /// <summary>
    /// Событие получения данных в режиме асинхронного чтения.
    /// </summary>
    event EventHandler<byte[]> DataReceived;

    /// <summary>
    /// Событие ошибки в потоке чтения (асинхронный режим). 
    /// После появления события чтение заканчивается.
    /// </summary>
    event EventHandler<Exception> ErrorInReadThread;

    /// <summary>
    /// Возращает значение указывающее на то, подключен ли сейчас клиент к какому либо хосту или нетю
    /// </summary>
    /// <returns>Значение true. если клиент подключен.</returns>
    bool IsConnected { get; }

    /// <summary>
    /// Возвращает или задает время ожидания окончания записи в милисекундах.
    /// </summary>
    /// <returns>Возращает время ожидания выполнения операции записи в милисекундах</returns>
    int WriteTimeout { get; set; }

    /// <summary>
    /// Возвращает или задает время ожидания данных для чтения в милисекундах.
    /// </summary>
    /// <returns>Возращает время ожидания появления данных для чтения в милисекундах</returns>
    int ReadTimeout { get; set; }

    /// <summary>
    /// Система уведомлений о приеме и передачи данных.
    /// </summary>
    NotificationSource Notifications { get; }

    /// <summary>
    /// Установка синхронного или асинхронного режима чтения.
    /// </summary>
    /// <param name="mode"></param>
    void SetReadMode(ReadMode mode);

    /// <summary>
    /// Подключение к указанному хосту.
    /// </summary>
    /// <param name="info"></param>
    void Connect(ConnectionInfo info);

    /// <summary>
    /// Закрытие открытого соединения.
    /// </summary>
    Task Disconnect();

    /// <summary>
    /// Запись определенного колличества байт в открытое соединение.
    /// </summary>
    /// <param name="message"></param>
    /// <param name="numberOfBytes"></param>
    Task<ModbusOperationInfo> Send(byte[] message, int numberOfBytes);

    /// <summary>
    /// Сихронно считывает данные из соединения. Возвращает принятые байты.
    /// </summary>
    Task<ModbusOperationInfo> Receive();
}
