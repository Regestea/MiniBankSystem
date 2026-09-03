using MiniBank.Domain.BuildingBlocks.Exceptions;

namespace MiniBank.Domain.BuildingBlocks.ValueObjects;

/// <summary>
/// Money ValueObject — USD only per requirements. Uses decimal for financial precision.
/// Immutable, no currency enum needed.
/// </summary>
public sealed record Money : IComparable<Money>
{
    public decimal Amount { get; }

    // USD only — currency is implicit
    private Money(decimal amount) => Amount = amount;

    public static Money FromDecimal(decimal amount)
    {
        if (amount < 0)
            throw new DomainValidationException(nameof(Money), "Amount cannot be negative.");

        // Ensure 2 decimal places max (cents)
        if (decimal.Round(amount, 2) != amount)
            throw new DomainValidationException(nameof(Money), "Amount cannot have more than 2 decimal places.");

        return new Money(decimal.Round(amount, 2));
    }

    public static Money Zero => new(0m);

    public Money Add(Money other) => FromDecimal(Amount + other.Amount);

    public Money Subtract(Money other)
    {
        if (Amount < other.Amount)
            throw new DomainInvariantViolationException(nameof(Money), "Insufficient funds.");
        return FromDecimal(Amount - other.Amount);
    }

    public bool IsZero => Amount == 0m;
    public bool IsPositive => Amount > 0m;

    public int CompareTo(Money? other) => other is null ? 1 : Amount.CompareTo(other.Amount);

    public static Money operator +(Money a, Money b) => a.Add(b);
    public static Money operator -(Money a, Money b) => a.Subtract(b);
    public static bool operator >(Money a, Money b) => a.Amount > b.Amount;
    public static bool operator <(Money a, Money b) => a.Amount < b.Amount;
    public static bool operator >=(Money a, Money b) => a.Amount >= b.Amount;
    public static bool operator <=(Money a, Money b) => a.Amount <= b.Amount;

    public override string ToString() => $"{Amount:F2} USD";
}
