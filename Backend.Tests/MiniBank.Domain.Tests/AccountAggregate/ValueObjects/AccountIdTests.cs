using MiniBank.Domain.AccountAggregate.ValueObjects;
using MiniBank.Domain.BuildingBlocks.Exceptions;

namespace MiniBank.Domain.Tests.AccountAggregate.ValueObjects;

public class AccountIdTests
{
    [Fact]
    public void Constructor_ValidGuid_Succeeds()
    {
        var guid = Guid.NewGuid();
        var id = new AccountId(guid);
        Assert.Equal(guid, (Guid)id);
    }

    [Fact]
    public void Constructor_EmptyGuid_ThrowsDomainValidationException()
    {
        Assert.Throws<DomainValidationException>(() => new AccountId(Guid.Empty));
    }

    [Fact]
    public void ImplicitConversion_FromGuid_CreatesAccountId()
    {
        Guid guid = Guid.NewGuid();
        AccountId id = guid;
        Assert.Equal(guid, id.Value);
    }

    [Fact]
    public void ImplicitConversion_ToGuid_ReturnsValue()
    {
        var guid = Guid.NewGuid();
        AccountId id = new(guid);
        Guid result = id;
        Assert.Equal(guid, result);
    }

    [Fact]
    public void ToString_ReturnsGuidString()
    {
        var guid = Guid.NewGuid();
        var id = new AccountId(guid);
        Assert.Equal(guid.ToString(), id.ToString());
    }

    [Fact]
    public void Equality_SameGuid_Equal()
    {
        var guid = Guid.NewGuid();
        var a = new AccountId(guid);
        var b = new AccountId(guid);
        Assert.Equal(a, b);
    }

    [Fact]
    public void Equality_DifferentGuid_NotEqual()
    {
        var a = new AccountId(Guid.NewGuid());
        var b = new AccountId(Guid.NewGuid());
        Assert.NotEqual(a, b);
    }
}
