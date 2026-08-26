using MiniBank.Domain.BuildingBlocks;

namespace MiniBank.Domain.Tests.BuildingBlocks;

internal sealed class TestValueObject : ValueObject
{
    public int Value1 { get; }
    public string? Value2 { get; }

    public TestValueObject(int v1, string? v2)
    {
        Value1 = v1;
        Value2 = v2;
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value1;
        yield return Value2;
    }
}

public class ValueObjectTests
{
    [Fact]
    public void Equals_SameValues_True()
    {
        var a = new TestValueObject(1, "hello");
        var b = new TestValueObject(1, "hello");
        Assert.Equal(a, b);
        Assert.True(a.Equals(b));
        Assert.True(a == b);
        Assert.False(a != b);
    }

    [Fact]
    public void Equals_DifferentValues_False()
    {
        var a = new TestValueObject(1, "hello");
        var b = new TestValueObject(2, "hello");
        var c = new TestValueObject(1, "world");
        Assert.NotEqual(a, b);
        Assert.NotEqual(a, c);
        Assert.False(a.Equals(b));
        Assert.True(a != b);
    }

    [Fact]
    public void Equals_Null_False()
    {
        var a = new TestValueObject(1, "hello");
        Assert.False(a.Equals(null));
        Assert.False(a.Equals((object?)null));
    }

    [Fact]
    public void Equals_DifferentType_False()
    {
        var a = new TestValueObject(1, "hello");
        var other = new DummyEntity(Guid.NewGuid()); // different type
        Assert.False(a.Equals(other));
    }

    [Fact]
    public void GetHashCode_SameValues_SameHash()
    {
        var a = new TestValueObject(42, "test");
        var b = new TestValueObject(42, "test");
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
    }

    [Fact]
    public void GetHashCode_DifferentValues_DifferentHash()
    {
        var a = new TestValueObject(1, "a");
        var b = new TestValueObject(2, "a");
        Assert.NotEqual(a.GetHashCode(), b.GetHashCode());
    }

    [Fact]
    public void Equality_WithNullComponent_Works()
    {
        var a = new TestValueObject(1, null);
        var b = new TestValueObject(1, null);
        var c = new TestValueObject(1, "notnull");
        Assert.Equal(a, b);
        Assert.NotEqual(a, c);
    }
}
