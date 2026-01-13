namespace Services.Interfaces;

public interface IOpenChildWindowService
{
    bool ChartWindowIsOpen { get; }
    Task Settings();
    Task<string?> UserInput();
    Task About();
    Task ModbusScanner();
    void Macros();
    Task<object?> EditMacros(object? parameters);
    Task<string?> EditFormula(string title, string? formula);
    void Chart();
}
