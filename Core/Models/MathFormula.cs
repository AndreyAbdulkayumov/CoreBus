using NCalc;
using System.Text.RegularExpressions;

namespace Core.Models;

public static class MathFormula
{
    public static double Solve(string formula, double xValue)
    {        
        var expression = CreateExpression(formula, xValue);

        object? result;

        try
        {
            // Вычисление результата
            result = expression.Evaluate();
        }
        
        catch (Exception error)
        {
            throw new Exception($"Заданная формула: {formula}\nx = {xValue}\n\n{error.Message}", error);
        }

        return Convert.ToDouble(result);
    }

    private static Expression CreateExpression(string? formula, double xValue = 0)
    {
        // Создаем выражение и игнорируем регистр у функций
        var expression = new Expression(formula, ExpressionOptions.IgnoreCaseAtBuiltInFunctions);

        // Устанавливаем параметры
        expression.Parameters["x"] = xValue;

        return expression;
    }

    // Паттерны для вставки умножения
    private static readonly Regex InsertMulBeforeX =
        new Regex(@"([0-9)])(x)", RegexOptions.Compiled);

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
        new Regex(@"^([0-9+\-*/(),.\s]|x\b|(abs|acos|asin|atan|cos|exp|log|pow|sin|sqrt|tan)\b)+$",
            RegexOptions.Compiled);

    // Запрет двойных операторов
    private static readonly Regex MultipleOperatorsPattern =
        new Regex(@"[+\-*/]{2}", RegexOptions.Compiled);

    // Неправильное начало формулы
    private static readonly Regex InvalidStartPattern =
        new Regex(@"^[+\*/)]", RegexOptions.Compiled);

    // Неправильное окончание формулы
    private static readonly Regex InvalidEndPattern =
        new Regex(@"[+\-*/(]\s*$", RegexOptions.Compiled);

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
                    "• Операторы: +, -, *, /\n" +
                    "• Скобки: (, )\n" +
                    "• Десятичная точка: .\n" +
                    "• Переменная: x (латиница)\n" +
                    "• Функции: pow, sqrt, abs, cos, sin, tan, acos, asin, atan, exp, log\n\n" +
                    "Примеры: 3.14, 2+3*(x-1), sin(x), sqrt(x+1)";

            return false;
        }

        if (MultipleOperatorsPattern.IsMatch(trimmed))
        {
            errorMessage = "Запрещены множественные операторы: *-, +*, //, +- и т.д.";
            return false;
        }
            
        if (InvalidStartPattern.IsMatch(trimmed))
        {
            errorMessage = "Формула не может начинаться с +, *, /, )";
            return false;
        }

        if (InvalidEndPattern.IsMatch(trimmed))
        {
            errorMessage = "Формула не может заканчиваться оператором +, -, *, /, (";
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

        if (!CanParsed(formula))
        {
            errorMessage = "Формула содержит ошибки.\nДробные числа записываются через точку: 5.12";
            return false;
        }

        return true;
    }

    private static bool AllBracketClosed(string formula)
    {
        int depth = 0;

        foreach (char symbol in formula)
        {
            switch (symbol)
            {
                case '(':
                    depth++;
                    break;

                case ')':
                    depth--;
                    break;
            }

            if (depth < 0) 
                return false; // закрывающая скобка раньше открывающей
        }

        return depth == 0;
    }

    private static bool CanParsed(string formula)
    {
        var expression = CreateExpression(Normalize(formula));

        if (expression.HasErrors())
            return false;

        // Проверим формулу на разных значениях
        var testValues = new[] { 1.0, 2.0, 0.5 };

        foreach (var x in testValues)
        {
            try
            {
                expression.Parameters["x"] = x;

                expression.Evaluate();

                return true; // достаточно одного успешного значения
            }

            catch (Exception)
            {
                // Игнорируем
            }
        }

        return false;
    }
}
