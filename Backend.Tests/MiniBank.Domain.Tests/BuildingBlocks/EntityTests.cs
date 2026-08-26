using MiniBank.Domain.BuildingBlocks;

namespace MiniBank.Domain.Tests.BuildingBlocks;

internal sealed class DummyEntity : Entity<Guid>
{
    public DummyEntity(Guid id) : base(id) { }
}

internal sealed class DummyEntityInt : Entity<int>
{
    public DummyEntityInt(int id) : base(id) { }
}

public class EntityTests
{
    [Fact]
    public void Equals_SameId_True()
    {
        var id = Guid.NewGuid();
        var a = new DummyEntity(id);
        var b = new DummyEntity(id);
        Assert.Equal(a, b);
        Assert.True(a.Equals(b));
        Assert.True(a == b);
        Assert.False(a != b);
    }

    [Fact]
    public void Equals_DifferentId_False()
    {
        var a = new DummyEntity(Guid.NewGuid());
        var b = new DummyEntity(Guid.NewGuid());
        Assert.NotEqual(a, b);
        Assert.False(a.Equals(b));
        Assert.False(a == b);
        Assert.True(a != b);
    }

    [Fact]
    public void Equals_Null_False()
    {
        var a = new DummyEntity(Guid.NewGuid());
        Assert.False(a.Equals(null));
        Assert.False(a.Equals((object?)null));
    }

    [Fact]
    public void GetHashCode_SameId_SameHash()
    {
        var id = Guid.NewGuid();
        var a = new DummyEntity(id);
        var b = new DummyEntity(id);
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
    }

    [Fact]
    public void GetHashCode_DifferentId_DifferentHash()
    {
        var a = new DummyEntity(Guid.NewGuid());
        var b = new DummyEntity(Guid.NewGuid());
        Assert.NotEqual(a.GetHashCode(), b.GetHashCode());
    }

    [Fact]
    public void Equality_WithIntId_Works()
    {
        var a = new DummyEntityInt(1);
        var b = new DummyEntityInt(1);
        var c = new DummyEntityInt(2);
        Assert.Equal(a, b);
        Assert.NotEqual(a, c);
    }

    [Fact]
    public void ReferenceEquals_SameInstance_True()
    {
        var a = new DummyEntity(Guid.NewGuid());
        DummyEntity b = a;
        Assert.True(ReferenceEquals(a, b));
        Assert.Equal(a, b);
    }
}
