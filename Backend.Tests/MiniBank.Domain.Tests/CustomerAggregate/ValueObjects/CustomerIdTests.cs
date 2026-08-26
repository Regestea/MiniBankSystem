using MiniBank.Domain.BuildingBlocks.Exceptions;
using MiniBank.Domain.CustomerAggregate.ValueObjects;

namespace MiniBank.Domain.Tests.CustomerAggregate.ValueObjects;

public class CustomerIdTests
{
    [Fact]
    public void Constructor_ValidGuid_Succeeds()
    {
        var guid = Guid.NewGuid();
        var id = new CustomerId(guid);
        Assert.Equal(guid, (Guid)id);
    }

    [Fact]
    public void Constructor_EmptyGuid_ThrowsDomainValidationException()
    {
        Assert.Throws<DomainValidationException>(() => new CustomerId(Guid.Empty));
    }

    [Fact]
    public void ImplicitConversion_FromGuid_CreatesCustomerId()
    {
        Guid guid = Guid.NewGuid();
        CustomerId id = guid;
        Assert.Equal(guid, id.Value);
    }

    [Fact]
    public void ImplicitConversion_ToGuid_ReturnsValue()
    {
        var guid = Guid.NewGuid();
        CustomerId id = new(guid);
        Guid result = id;
        Assert.Equal(guid, result);
    }

    [Fact]
    public void ToString_ReturnsGuidString()
    {
        var guid = Guid.NewGuid();
        var id = new CustomerId(guid);
        Assert.Equal(guid.ToString(), id.ToString());
    }

    [Fact]
    public void Equality_SameValue_Equal()
    {
        var guid = Guid.NewGuid();
        var a = new CustomerId(guid);
        var b = new CustomerId(guid);
        Assert.Equal(a, b);
    }

    [Fact]
    public void Equality_DifferentValue_NotEqual()
    {
        var a = new CustomerId(Guid.NewGuid());
        var b = new CustomerId(Guid.NewGuid());
        Assert.NotEqual(a, b);
    }

    [Fact]
    public void ImplicitOperator_GuidEmpty_ThrowsViaValidation()
    {
        Assert.Throws<DomainValidationException>(() => (CustomerId)Guid.Empty);
    }
}
