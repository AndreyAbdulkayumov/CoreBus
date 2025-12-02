namespace ViewModels.ModbusClient.Manual.ModbusRepresentations;

internal static class LogRepresentation
{
    public static string GetData(
        DateTime request_ExecutionTime, string requestString,
        DateTime response_ExecutionTime, string responseString)
    {
        string data = string.Empty;

        if (requestString != string.Empty)
        {
            data += request_ExecutionTime.ToString("HH : mm : ss . fff") + "   ->   " + requestString;
        }

        if (responseString != string.Empty)
        {
            if (requestString != string.Empty)
            {
                data += "\n";
            }

            data += response_ExecutionTime.ToString("HH : mm : ss . fff") + "   <-   " + responseString;
        }

        return data;
    }
}
