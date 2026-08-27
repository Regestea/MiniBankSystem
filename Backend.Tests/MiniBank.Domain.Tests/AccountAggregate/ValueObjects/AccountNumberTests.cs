using MiniBank.Domain.BuildingBlocks.Exceptions;
using MiniBank.Domain.AccountAggregate.ValueObjects;

namespace MiniBank.Domain.Tests.AccountAggregate.ValueObjects;

public class AccountNumberTests
{
    [Theory]
    [InlineData("1234567890123456")]
    [InlineData("1000000000000000")]
    [InlineData("9999999999999999")]
    public void Constructor_ValidNumber_Succeeds(string valid)
    {
        var number = new AccountNumber(valid);
        Assert.Equal(valid, (string)number);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void Constructor_Empty_ThrowsDomainValidationException(string? invalid)
    {
        Assert.Throws<DomainValidationException>(() => new AccountNumber(invalid!));
    }

    [Theory]
    [InlineData("0234567890123456")] // starts with 0
    [InlineData("123456789012345")]  // 15 digits
    [InlineData("12345678901234567")] // 17 digits
    [InlineData("123456789012345A")] // contains letter
    [InlineData("1234 567890123456")] // space
    [InlineData("123456789012345-")] // dash
    public void Constructor_InvalidFormat_ThrowsDomainValidationException(string invalid)
    {
        Assert.Throws<DomainValidationException>(() => new AccountNumber(invalid));
    }

    [Fact]
    public void Generate_CreatesValidNumber()
    {
        var number = AccountNumber.Generate();
        Assert.Matches("^[1-9]\\d{15}$", number.Value);
    }

    [Fact]
    public void Generate_CreatesUniqueNumbers()
    {
        var a = AccountNumber.Generate();
        var b = AccountNumber.Generate();
        // Very high probability unique; if collision, test flaky but acceptable
        Assert.NotEqual(a.Value, b.Value);
    }

    [Fact]
    public void ImplicitConversion_ToString_ReturnsValue()
    {
        var number = new AccountNumber("1234567890123456");
        string value = number;
        Assert.Equal("1234567890123456", value);
    }

    [Fact]
    public void ImplicitConversion_FromString_CreatesAccountNumber()
    {
        string raw = "1234567890123456";
        AccountNumber number = raw;
        Assert.Equal(raw, number.Value);
    }

    [Fact]
    public void ToString_ReturnsValue()
    {
        var number = new AccountNumber("1234567890123456");
        Assert.Equal("1234567890123456", number.ToString());
    }

    [Fact]
    public void Constructor_TrimsWhitespace()
    {
        var number = new AccountNumber("  1234567890123456  ");
        Assert.Equal("1234567890123456", number.Value);
    }

    [Fact]
    public void Equality_SameValue_Equal()
    {
        var a = new AccountNumber("1234567890123456");
        var b = new AccountNumber("1234567890123456");
        Assert.Equal(a, b);
    }
}
