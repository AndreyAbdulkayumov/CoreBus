using NCalc;
using System.Text.RegularExpressions;

namespace Core.Models;

public static class MathFormula
{
    public static double Solve(string formula, double xValue)
    {
        // Создаем выражение
        var expression = new Expression(formula);

        // Устанавливаем параметры
        expression.Parameters["x"] = xValue;

        // Вычисление результата
        var result = expression.Evaluate();

        return Convert.ToDouble(result);
    }

    // Паттерны для вставки умножения
    private static readonly Regex InsertMulBeforeX =
        new Regex(@"([0-9)]|\))(x)", RegexOptions.Compiled);

    private static readonly Regex InsertMulAfterX =
        new Regex(@"(x)([0-9(])", RegexOptions.Compiled);

    public static string? Normalize(string? formula)
    {
        if (string.IsNullOrWhiteSpace(formula))
            return formula;

        string trimmed = formula.Trim();

        // 1. Вставка * перед x: 2x → 2*x, )x → )*x
        string step1 = InsertMulBeforeX.Replace(trimmed, "$1*$2");

        // 2. Вставка * после x: x2 → x*2, x( → x*( 
        string normalized = InsertMulAfterX.Replace(step1, "$1*$2");

        return normalized;
    }

    // Базовая проверка символов
    private static readonly Regex AllowedCharsPattern =
        new Regex(@"^[0-9+\-*/^().\sx]+$", RegexOptions.Compiled);

    // Запрет двойных операторов
    private static readonly Regex MultipleOperatorsPattern =
        new Regex(@"[+\-*/^]{2}", RegexOptions.Compiled);

    // Неправильное начало формулы
    private static readonly Regex InvalidStartPattern =
        new Regex(@"^[+\*/^)]", RegexOptions.Compiled);

    // Неправильное окончание формулы
    private static readonly Regex InvalidEndPattern =
        new Regex(@"[+\-*/^(]\s*$", RegexOptions.Compiled);

    // Пропущенные операторы у цифр перед или после скобок
    private static readonly Regex MissingOperatorBrackets =
        new Regex(@"([0-9]+)(\()|(\))([0-9]+)", RegexOptions.Compiled);

    public static bool IsValid(string formula, out string errorMessage)
    {
        errorMessage = string.Empty;

        var trimmed = formula.Trim();

        if (!AllowedCharsPattern.IsMatch(trimmed))
        {
            errorMessage = "Разрешены только:\n" +
                    "• Цифры: 0-9\n" +
                    "• Операторы: +, -, *, /, ^\n" +
                    "• Скобки: (, )\n" +
                    "• Десятичная точка: .\n" +
                    "• Переменная: x (латиница)\n\n" +
                    "Примеры: x^2, 2+3*(x-1), 3.14";

            return false;
        }

        if (MultipleOperatorsPattern.IsMatch(trimmed))
        {
            errorMessage = "Запрещены множественные операторы: *-, +*, //, +- и т.д.";

            return false;
        }
            
        if (InvalidStartPattern.IsMatch(trimmed))
        {
            errorMessage = "Формула не может начинаться с +, *, /, ^, )";

            return false;
        }

        if (InvalidEndPattern.IsMatch(trimmed))
        {
            errorMessage = "Формула не может заканчиваться оператором +, -, *, /, ^, (";

            return false;
        }

        if (!AllBracketClosed(formula))
        {
            errorMessage = "Не совпадает количество открытых и закрытых скобок";

            return false;
        }

        if (MissingOperatorBrackets.IsMatch(trimmed))
        {
            errorMessage = "У цифры перед или после скобки не хватает оператора";

            return false;
        }

        return true;
    }

    private static bool AllBracketClosed(string formula)
    {
        int openCount = 0, closeCount = 0;

        foreach (char symbol in formula)
        {
            switch (symbol)
            {
                case '(':
                    openCount++;
                    break;

                case ')':
                    closeCount++;
                    break;
            }
        }

        return openCount == closeCount;
    }
}
