using Core.Models;
using Core.Tests.Infrastructure;

namespace Core.Tests;

public class MathFormula_Test
{
    private const int DoublePrecision = 10;

    public MathFormula_Test()
    {
        LocalizationProvider.Instance = new TestLocalizationService();
    }

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
    public void Solve_BasicExpressions_ReturnsExpected(string formula, double xValue, double expected)
    {
        var actual = MathFormula.Solve(formula, xValue);

        Assert.Equal(expected, actual, DoublePrecision);
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
}
