namespace Services.Interfaces;

public interface IOpenChildWindowService
{
    Task Settings();
    Task<string?> UserInput();
    Task About();
    Task ModbusScanner();
    void Macros();
    Task<object?> EditMacros(object? parameters);
    Task<string?> EditFormula(string title, string? formula);
    void Chart();
}
