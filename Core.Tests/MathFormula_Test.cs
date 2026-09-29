using Core.Models;

namespace Core.Tests;

public class MathFormula_Test
{
    private const int DoublePrecision = 10;

    [Theory]
    [InlineData("x+5", 4.0, 9.0)]
    [InlineData("x-5", 4.0, -1.0)]
    [InlineData("x*2", 4.0, 8.0)]
    [InlineData("x/2", 4.0, 2.0)]
    [InlineData("abs(-5)", 0.0, 5.0)]
    [InlineData("sqrt(x)", 16.0, 4.0)]
    [InlineData("pow(x,2)", 3.0, 9.0)]
    [InlineData("exp(0)", 0.0, 1.0)]
    [InlineData("log(100,10)", 0.0, 2.0)]
    [InlineData("sin(0)", 0.0, 0.0)]
    [InlineData("cos(0)", 0.0, 1.0)]
    [InlineData("tan(0)", 0.0, 0.0)]
    [InlineData("asin(0)", 0.0, 0.0)]
    [InlineData("acos(1)", 0.0, 0.0)]
    [InlineData("atan(0)", 0.0, 0.0)]
    public void Solve_BasicExpressions_ReturnsExpected(string formula, double xValue, double expectedValue)
    {
        var actualValue = MathFormula.Solve(formula, xValue);

        Assert.Equal(expectedValue, actualValue, DoublePrecision);
    }

    [Theory]
    [InlineData("SIN(0)")]
    [InlineData("Sin(0)")]
    [InlineData("sin(0)")]
    public void Solve_IgnoresFunctionCase(string formula)
    {
        Assert.Equal(0.0, MathFormula.Solve(formula, 0.0), DoublePrecision);
    }

    [Theory]
    [InlineData("(")]
    [InlineData("")]
    [InlineData("foo(1)")]
    public void Solve_WhenEvaluationFails_ThrowsWrappedException(string formula)
    {
        const double xValue = 1.0;

        Assert.Throws<Exception>(() => 
            MathFormula.Solve(formula, xValue));
    }

    [Theory]
    [InlineData("(x-4)/16*100", 4.0, 0.0)]
    [InlineData("(x-4)/16*100", 12.0, 50.0)]
    [InlineData("x/(12*x+10)", 20.0, 0.08)]
    [InlineData("x*0.1+25", 100.0, 35.0)]
    public void Solve_DomainFormulas_ReturnsExpected(string formula, double xValue, double expectedResult)
    {
        var actualResult = MathFormula.Solve(formula, xValue);

        Assert.Equal(expectedResult, actualResult, DoublePrecision);
    }

    [Theory]
    [InlineData("2x", "2*x")]
    [InlineData("(1+2)x", "(1+2)*x")]
    [InlineData("x2", "x*2")]
    [InlineData("x(1+2)", "x*(1+2)")]
    [InlineData("2x3", "2*x*3")]
    [InlineData("2x(3)", "2*x*(3)")]
    [InlineData("(2+3)x(4)", "(2+3)*x*(4)")]
    public void Normalize_InsertsMultiplicationAroundX(string formula, string expected)
    {
        Assert.Equal(expected, MathFormula.Normalize(formula));
    }

    [Theory]
    [InlineData("x")]
    [InlineData("2*x")]
    [InlineData("x*2")]
    [InlineData("sin(x)")]
    [InlineData("2*x+1")]
    [InlineData("(x-4)/16*100")]
    public void Normalize_WhenAlreadyExplicit_ReturnsUnchanged(string formula)
    {
        Assert.Equal(formula, MathFormula.Normalize(formula));
    }

    [Theory]
    [InlineData("  2x  ", "2*x")]
    [InlineData("\tx(1)\t", "x*(1)")]
    public void Normalize_TrimsAndInsertsMultiplication(string formula, string expected)
    {
        Assert.Equal(expected, MathFormula.Normalize(formula));
    }

    [Theory]
    [InlineData("x")]
    [InlineData("-x")]
    [InlineData("2x")]
    [InlineData("2*x+1")]
    [InlineData("sin(x)")]
    [InlineData("abs(-5)")]
    [InlineData("log(x,10)")]
    [InlineData("x(2)")]
    [InlineData("(x-4)/16*100")]
    [InlineData("  x+1  ")]
    public void IsValid_ValidFormula(string formula)
    {
        var isValid = MathFormula.IsValid(formula, out var errorMessage);

        Assert.True(isValid);
        Assert.Equal(string.Empty, errorMessage);
    }

    [Theory]
    [InlineData("y", "Core.FormulaAllowedChars")]
    [InlineData("X", "Core.FormulaAllowedChars")]
    [InlineData("foo(x)", "Core.FormulaAllowedChars")]
    [InlineData("2&x", "Core.FormulaAllowedChars")]
    [InlineData("", "Core.FormulaAllowedChars")]
    [InlineData("x++1", "Core.FormulaMultipleOperators")]
    [InlineData("2+-3", "Core.FormulaMultipleOperators")]
    [InlineData("x*/2", "Core.FormulaMultipleOperators")]
    [InlineData("+x", "Core.FormulaInvalidStart")]
    [InlineData("*x", "Core.FormulaInvalidStart")]
    [InlineData("/x", "Core.FormulaInvalidStart")]
    [InlineData(")x", "Core.FormulaInvalidStart")]
    [InlineData("x+", "Core.FormulaInvalidEnd")]
    [InlineData("x-", "Core.FormulaInvalidEnd")]
    [InlineData("x*", "Core.FormulaInvalidEnd")]
    [InlineData("x/", "Core.FormulaInvalidEnd")]
    [InlineData("x(", "Core.FormulaInvalidEnd")]
    [InlineData("(x", "Core.FormulaBracketMismatch")]
    [InlineData("x)", "Core.FormulaBracketMismatch")]
    [InlineData("((x)", "Core.FormulaBracketMismatch")]
    [InlineData("2(x)", "Core.FormulaMissingOperatorNearBracket")]
    [InlineData("(x)2", "Core.FormulaMissingOperatorNearBracket")]
    [InlineData("log(x)", "Core.FormulaParseError")]
    [InlineData("pow(x)", "Core.FormulaParseError")]
    public void IsValid_InvalidFormula(string formula, string expectedErrorKey)
    {
        // Тут LocalizationProvider.Get() вернет ключ, а не локализованную строку.
        // В errorMessage будет ключ.
        var isValid = MathFormula.IsValid(formula, out var errorMessage);

        Assert.False(isValid);
        Assert.Equal(expectedErrorKey, errorMessage);
    }
}
