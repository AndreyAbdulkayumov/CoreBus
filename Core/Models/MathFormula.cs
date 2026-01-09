using NCalc;
using System.ComponentModel.DataAnnotations;
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

    // Базовая проверка символов
    private static readonly Regex AllowedCharsPattern =
        new Regex(@"^[0-9+\-*/^().\sXx]+$", RegexOptions.Compiled);

    // Запрет двойных операторов
    private static readonly Regex MultipleOperatorsPattern =
        new Regex(@"[+\-*/^]{2}", RegexOptions.Compiled);

    // Неправильное начало формулы
    private static readonly Regex InvalidStartPattern =
        new Regex(@"^[+\*/^)]", RegexOptions.Compiled);

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
                    "• Переменная: x или X (лат.)\n\n" +
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

        if (!AllBracketClosed(formula))
        {
            errorMessage = "Не совпадает количество открытых и закрытых скобок";

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
