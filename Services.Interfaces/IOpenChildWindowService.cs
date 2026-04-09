namespace Services.Interfaces;

public interface IOpenChildWindowService
{
    bool ChartWindowIsOpen { get; }
    bool MacrosWindowIsOpen { get; }
    Task Settings();
    Task<string?> UserInput();
    Task About();
    Task ModbusScanner();
    void Macros();
    Task<object?> EditMacros(object? parameters);
    Task<string?> EditFormula(string description, string? formula, bool isEnable);
    void Chart();
    void RaiseChartWindow();
}
