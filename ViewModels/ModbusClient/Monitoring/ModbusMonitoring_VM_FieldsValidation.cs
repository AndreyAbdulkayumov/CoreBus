using System.Globalization;
using System.Text;
using ViewModels.Validation;

namespace ViewModels.ModbusClient.Monitoring;

public partial class ModbusMonitoring_VM : ValidatedDateInput, IValidationFieldInfo
{
    private string? CheckFields()
    {
        var validationMessages = new StringBuilder();

        // Проверка полей в настройках мониторинга
        foreach (KeyValuePair<string, ValidateMessage> element in ActualErrors)
        {
            validationMessages.AppendLine($"[{GetFieldViewName(element.Key)}]\n{GetFullErrorMessage(element.Key)}\n");
        }

        // Проверка полей в таблице
        for (int i = 0; i < MonitoringItems.Count; i++)
        {
            var checkedItem = MonitoringItems[i];

            foreach (KeyValuePair<string, ValidateMessage> itemElement in checkedItem.ActualErrors)
            {
                validationMessages.AppendLine($"[Элемент {i + 1} - {checkedItem.GetFieldViewName(itemElement.Key)}]\n{checkedItem.GetFullErrorMessage(itemElement.Key)}\n");
            }
        }

        if (validationMessages.Length > 0)
        {
            validationMessages.Insert(0, "Ошибки валидации:\n\n");
            return validationMessages.ToString().TrimEnd('\r', '\n');
        }

        return null;
    }

    public string GetFieldViewName(string fieldName)
    {
        switch (fieldName)
        {
            case nameof(SlaveID):
                return "Slave ID";

            case nameof(Period_ms):
                return "Период";

            default:
                return fieldName;
        }
    }

    protected override ValidateMessage? GetErrorMessage(string fieldName, string? value)
    {
        switch (fieldName)
        {
            case nameof(SlaveID):
                return Check_SlaveID(value);

            case nameof(Period_ms):
                return Check_Period(value);
        }

        return null;
    }

    private ValidateMessage? Check_SlaveID(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return AllErrorMessages[NotEmptyField];
        }

        if (!StringValue.IsValidNumber(value, _numberViewStyle, out _selectedSlaveID))
        {
            switch (_numberViewStyle)
            {
                case NumberStyles.Number:
                    return AllErrorMessages[DecError_Byte];

                case NumberStyles.HexNumber:
                    return AllErrorMessages[HexError_Byte];
            }
        }

        return null;
    }

    private ValidateMessage? Check_Period(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return AllErrorMessages[NotEmptyField];
        }

        if (!StringValue.IsValidNumber(value, NumberStyles.Number, out uint _))
        {
            return AllErrorMessages[DecError_uint];
        }

        return null;
    }
}
