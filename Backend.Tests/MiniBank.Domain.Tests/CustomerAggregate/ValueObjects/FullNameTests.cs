using MiniBank.Domain.BuildingBlocks.Exceptions;
using MiniBank.Domain.CustomerAggregate.ValueObjects;
using Xunit;

namespace MiniBank.Domain.Tests.CustomerAggregate.ValueObjects;

public class FullNameTests
{
    [Theory]
    [InlineData("John Doe")]
    [InlineData("Alice Smith")]
    [InlineData("Mary Jane Watson")]
    [InlineData("A B")]
    [InlineData("Firstname Lastname")]
    public void FullName_ValidFullName_Succeeds(string validName)
    {
        var fullName = new FullName(validName);
        Assert.Equal(validName.Trim(), (string)fullName);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void FullName_EmptyOrWhitespace_ThrowsDomainValidationException(string? invalidName)
    {
        Assert.Throws<DomainValidationException>(() => new FullName(invalidName!));
    }

    [Theory]
    [InlineData("J@hn Doe")]
    [InlineData("John123")]
    [InlineData("John_Doe")]
    [InlineData("JohnDoe!")]
    [InlineData("John#Doe")]
    public void FullName_InvalidCharacters_ThrowsDomainValidationException(string invalidName)
    {
        Assert.Throws<DomainValidationException>(() => new FullName(invalidName));
    }

    [Theory]
    [InlineData("Jean-Luc Picard")]
    [InlineData("O'Brien")]
    [InlineData("Dr. John Smith")]
    [InlineData("علی رضایی")]
    public void FullName_InternationalNames_Succeeds(string validName)
    {
        var fullName = new FullName(validName);
        Assert.Equal(validName.Trim(), (string)fullName);
    }

    [Theory]
    [InlineData("A")] 
    [InlineData(" ")]
    [InlineData("ThisNameIsWayTooLongForTheSystemToAcceptBecauseItExceedsTheMaximumAllowedLengthOfOneHundredCharactersWhichIsNotValid")] 
    public void FullName_InvalidLength_ThrowsDomainValidationException(string invalidName)
    {
        Assert.Throws<DomainValidationException>(() => new FullName(invalidName));
    }

    [Fact]
    public void ImplicitConversion_ToString_ReturnsValue()
    {
        FullName fullName = "John Doe";
        string value = fullName;
        Assert.Equal("John Doe", value);
    }

    [Fact]
    public void ImplicitConversion_FromString_CreatesFullName()
    {
        string name = "Jane Doe";
        FullName fullName = name;
        Assert.Equal(name, (string)fullName);
    }
}