using MiniBank.Domain.BuildingBlocks.Exceptions;
using MiniBank.Domain.BuildingBlocks.ValueObjects;

namespace MiniBank.Domain.Tests.BuildingBlocks.ValueObjects;

public class MoneyTests
{
    [Fact]
    public void FromDecimal_ValidAmount_CreatesMoney()
    {
        var money = Money.FromDecimal(100.50m);
        Assert.Equal(100.50m, money.Amount);
    }

    [Fact]
    public void FromDecimal_Zero_CreatesZeroMoney()
    {
        var money = Money.FromDecimal(0m);
        Assert.True(money.IsZero);
        Assert.Equal(0m, money.Amount);
    }

    [Fact]
    public void Zero_ReturnsZero()
    {
        var zero = Money.Zero;
        Assert.True(zero.IsZero);
        Assert.Equal(0m, zero.Amount);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(-0.01)]
    [InlineData(-100)]
    public void FromDecimal_Negative_ThrowsDomainValidationException(decimal amount)
    {
        Assert.Throws<DomainValidationException>(() => Money.FromDecimal(amount));
    }

    [Theory]
    [InlineData(10.123)]
    [InlineData(0.001)]
    [InlineData(100.1234)]
    public void FromDecimal_MoreThanTwoDecimals_ThrowsDomainValidationException(decimal amount)
    {
        Assert.Throws<DomainValidationException>(() => Money.FromDecimal(amount));
    }

    [Fact]
    public void Add_Valid_SumsAmounts()
    {
        var a = Money.FromDecimal(100m);
        var b = Money.FromDecimal(50.25m);
        var result = a.Add(b);
        Assert.Equal(150.25m, result.Amount);
    }

    [Fact]
    public void Add_UsingOperator_SumsAmounts()
    {
        Money a = 100m;
        Money b = 50m;
        Money result = a + b;
        Assert.Equal(150m, result.Amount);
    }

    [Fact]
    public void Subtract_Valid_SubtractsAmounts()
    {
        var a = Money.FromDecimal(100m);
        var b = Money.FromDecimal(30m);
        var result = a.Subtract(b);
        Assert.Equal(70m, result.Amount);
    }

    [Fact]
    public void Subtract_InsufficientFunds_ThrowsDomainInvariantViolationException()
    {
        var a = Money.FromDecimal(50m);
        var b = Money.FromDecimal(100m);
        Assert.Throws<DomainInvariantViolationException>(() => a.Subtract(b));
    }

    [Fact]
    public void Subtract_UsingOperator_SubtractsAmounts()
    {
        Money a = 100m;
        Money b = 30m;
        Money result = a - b;
        Assert.Equal(70m, result.Amount);
    }

    [Theory]
    [InlineData(100, 50, true)]
    [InlineData(50, 100, false)]
    [InlineData(100, 100, false)]
    public void GreaterThan_ComparesCorrectly(decimal a, decimal b, bool expected)
    {
        var moneyA = Money.FromDecimal(a);
        var moneyB = Money.FromDecimal(b);
        Assert.Equal(expected, moneyA > moneyB);
    }

    [Theory]
    [InlineData(50, 100, true)]
    [InlineData(100, 50, false)]
    public void LessThan_ComparesCorrectly(decimal a, decimal b, bool expected)
    {
        var moneyA = Money.FromDecimal(a);
        var moneyB = Money.FromDecimal(b);
        Assert.Equal(expected, moneyA < moneyB);
    }

    [Fact]
    public void ImplicitConversion_FromDecimal_CreatesMoney()
    {
        Money money = 123.45m;
        Assert.Equal(123.45m, money.Amount);
    }

    [Fact]
    public void ImplicitConversion_ToDecimal_ReturnsAmount()
    {
        var money = Money.FromDecimal(99.99m);
        decimal amount = money;
        Assert.Equal(99.99m, amount);
    }

    [Fact]
    public void IsPositive_TrueForPositive()
    {
        var money = Money.FromDecimal(0.01m);
        Assert.True(money.IsPositive);
    }

    [Fact]
    public void IsPositive_FalseForZero()
    {
        Assert.False(Money.Zero.IsPositive);
    }

    [Fact]
    public void ToString_ReturnsFormattedUsd()
    {
        var money = Money.FromDecimal(1234.5m);
        Assert.Equal("1234.50 USD", money.ToString());
    }

    [Fact]
    public void FromDecimal_TrimsTrailingZeros_Correctly()
    {
        // Already rounded to 2 decimals
        var money = Money.FromDecimal(100m);
        Assert.Equal(100.00m, money.Amount);
    }
}
