using MiniBank.Domain.BuildingBlocks.Exceptions;
using MiniBank.Domain.CustomerAggregate.ValueObjects;
using Xunit;

namespace MiniBank.Domain.Tests.CustomerAggregate.ValueObjects;

public class PhoneNumberTests
{
    [Theory]
    [InlineData("0123456789")]
    [InlineData("1234567890")]
    [InlineData("123456789012345")]
    [InlineData("0987654321123")]
    public void PhoneNumber_ValidPhoneNumber_Success(string validNumber)
    {
        PhoneNumber phoneNumber = validNumber;
        Assert.Equal(validNumber, phoneNumber);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void Constructor_EmptyOrWhitespace_Throws(string? invalidNumber)
    {
        Assert.Throws<DomainValidationException>(() => new PhoneNumber(invalidNumber!));
    }

    [Theory]
    [InlineData("12345abcde")]
    [InlineData("123-456-7890")]
    [InlineData("123 456 7890")]
    [InlineData("(123)4567890")]
    [InlineData("+1234567890")]
    public void Constructor_NonDigitCharacters_Throws(string invalidNumber)
    {
        Assert.Throws<DomainValidationException>(() => new PhoneNumber(invalidNumber));
    }

    [Theory]
    [InlineData("123456789")] // 9 digits
    [InlineData("1234567890123456")] // 16 digits
    public void Constructor_InvalidLength_Throws(string invalidNumber)
    {
        Assert.Throws<DomainValidationException>(() => new PhoneNumber(invalidNumber));
    }

    [Fact]
    public void ImplicitConversion_ToString_ReturnsValue()
    {
        PhoneNumber phoneNumber = "0123456789";
        string value = phoneNumber;
        Assert.Equal("0123456789", value);
    }

    [Fact]
    public void ImplicitConversion_FromString_CreatesPhoneNumber()
    {
        string number = "1234567890";
        PhoneNumber phoneNumber = number;
        Assert.Equal(number,phoneNumber);
    }
}