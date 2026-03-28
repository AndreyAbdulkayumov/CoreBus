namespace ViewModels.Validation;

public class ValidateMessage
{
    public readonly string Short;
    public readonly string Full;

    public ValidateMessage(string shortMessage, string fullMessage)
    {
        Short = shortMessage;
        Full = fullMessage;
    }

    public ValidateMessage(string message)
    {
        Short = message;
        Full = message;
    }
}
