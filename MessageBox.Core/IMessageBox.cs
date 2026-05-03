namespace MessageBox.Core;

public enum MessageType
{
    Error,
    Warning,
    Information
}

public enum MessageBoxResult
{
    Default,
    Yes,
    No
}

public interface IMessageBox
{
    void Show(string message, MessageType type, Exception? error = null);
    Task ShowDialog(string message, MessageType messageType, Exception? error = null);
    Task<MessageBoxResult> ShowYesNoDialog(string message, MessageType type, Exception? error = null);
}
