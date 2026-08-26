using MiniBank.Domain.BuildingBlocks.Exceptions;
using MiniBank.Domain.TransactionAggregate.ValueObjects;

namespace MiniBank.Domain.Tests.TransactionAggregate.ValueObjects;

public class TransactionIdTests
{
    [Fact]
    public void Constructor_ValidGuid_Succeeds()
    {
        var guid = Guid.NewGuid();
        var id = new TransactionId(guid);
        Assert.Equal(guid, (Guid)id);
    }

    [Fact]
    public void Constructor_EmptyGuid_ThrowsDomainValidationException()
    {
        Assert.Throws<DomainValidationException>(() => new TransactionId(Guid.Empty));
    }

    [Fact]
    public void ImplicitConversion_FromGuid_CreatesTransactionId()
    {
        Guid guid = Guid.NewGuid();
        TransactionId id = guid;
        Assert.Equal(guid, id.Value);
    }

    [Fact]
    public void ImplicitConversion_ToGuid_ReturnsValue()
    {
        var guid = Guid.NewGuid();
        TransactionId id = new(guid);
        Guid result = id;
        Assert.Equal(guid, result);
    }

    [Fact]
    public void ToString_ReturnsGuidString()
    {
        var guid = Guid.NewGuid();
        var id = new TransactionId(guid);
        Assert.Equal(guid.ToString(), id.ToString());
    }

    [Fact]
    public void Equality_SameGuid_Equal()
    {
        var guid = Guid.NewGuid();
        var a = new TransactionId(guid);
        var b = new TransactionId(guid);
        Assert.Equal(a, b);
    }

    [Fact]
    public void Equality_DifferentGuid_NotEqual()
    {
        var a = new TransactionId(Guid.NewGuid());
        var b = new TransactionId(Guid.NewGuid());
        Assert.NotEqual(a, b);
    }
}
