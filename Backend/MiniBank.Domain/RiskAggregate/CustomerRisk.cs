using MiniBank.Domain.BuildingBlocks;
using MiniBank.Domain.BuildingBlocks.Exceptions;
using MiniBank.Domain.RiskAggregate.Events;
using MiniBank.Domain.RiskAggregate.ValueObjects;

namespace MiniBank.Domain.RiskAggregate;

public sealed class CustomerRisk : AggregateRoot<CustomerRiskId>
{
    public Guid CustomerId { get; private set; }
    public RiskLevel RiskLevel { get; private set; }
    public decimal DailyTransactionLimit { get; private set; }
    public int DailyTransactionCountLimit { get; private set; }
    public int TransactionsToday { get; private set; }
    public decimal AmountToday { get; private set; }
    public DateTimeOffset LastResetDate { get; private set; }

    private CustomerRisk() { }

    private CustomerRisk(CustomerRiskId id, Guid customerId)
        : base(id)
    {
        CustomerId = customerId;
        RiskLevel = RiskLevel.Low;
        DailyTransactionLimit = 10_000m;
        DailyTransactionCountLimit = 10;
        TransactionsToday = 0;
        AmountToday = 0m;
        LastResetDate = DateTimeOffset.UtcNow;
        CreatedAt = DateTimeOffset.UtcNow;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public static CustomerRisk Create(Guid customerId, CustomerRiskId? id = null)
    {
        if (customerId == Guid.Empty)
            throw new DomainValidationException(nameof(customerId), "CustomerId cannot be empty.");

        id ??= new CustomerRiskId(Guid.NewGuid());
        return new CustomerRisk(id, customerId);
    }

    public void RecordTransaction(decimal amount)
    {
        if (amount <= 0)
            throw new DomainValidationException(nameof(amount), "Amount must be positive.");

        ResetDailyCountersIfNeeded();

        TransactionsToday++;
        AmountToday += amount;
        IncrementVersion();
    }

    public bool CanTransact(decimal amount)
    {
        var (effectiveTransactions, effectiveAmount) = GetEffectiveCounters();

        if (effectiveTransactions >= DailyTransactionCountLimit)
            return false;

        if (effectiveAmount + amount > DailyTransactionLimit)
            return false;

        return true;
    }

    private (int transactionsToday, decimal amountToday) GetEffectiveCounters()
    {
        if (DateOnly.FromDateTime(LastResetDate.Date) < DateOnly.FromDateTime(DateTimeOffset.UtcNow.Date))
            return (0, 0m);

        return (TransactionsToday, AmountToday);
    }

    public void SetRiskLevel(RiskLevel level, Guid? changedBy = null)
    {
        var oldLevel = RiskLevel;
        RiskLevel = level;
        UpdateLimitsForLevel(level);
        // NOTE: daily counters are intentionally NOT reset here. Resetting on every
        // level change would let an admin (or a compromised admin flow) wipe
        // TransactionsToday/AmountToday and evade High-risk limits. Counters reset
        // only on day rollover via ResetDailyCountersIfNeeded.
        IncrementVersion();

        if (oldLevel != level)
            AddDomainEvent(new RiskLevelChangedEvent(Id, CustomerId, oldLevel, level));
    }

    private void UpdateLimitsForLevel(RiskLevel level)
    {
        switch (level)
        {
            case RiskLevel.Low:
                DailyTransactionLimit = 10_000m;
                DailyTransactionCountLimit = 10;
                break;
            case RiskLevel.Medium:
                DailyTransactionLimit = 5_000m;
                DailyTransactionCountLimit = 5;
                break;
            case RiskLevel.High:
                DailyTransactionLimit = 1_000m;
                DailyTransactionCountLimit = 3;
                break;
        }
    }

    private void ResetDailyCountersIfNeeded()
    {
        if (DateOnly.FromDateTime(LastResetDate.Date) < DateOnly.FromDateTime(DateTimeOffset.UtcNow.Date))
        {
            ResetDailyCounters();
        }
    }

    private void ResetDailyCounters()
    {
        TransactionsToday = 0;
        AmountToday = 0m;
        LastResetDate = DateTimeOffset.UtcNow;
    }

    private CustomerRisk(
        CustomerRiskId id,
        Guid customerId,
        RiskLevel riskLevel,
        decimal dailyTransactionLimit,
        int dailyTransactionCountLimit,
        int transactionsToday,
        decimal amountToday,
        DateTimeOffset lastResetDate,
        int version,
        DateTimeOffset createdAt,
        DateTimeOffset updatedAt)
        : base(id)
    {
        CustomerId = customerId;
        RiskLevel = riskLevel;
        DailyTransactionLimit = dailyTransactionLimit;
        DailyTransactionCountLimit = dailyTransactionCountLimit;
        TransactionsToday = transactionsToday;
        AmountToday = amountToday;
        LastResetDate = lastResetDate;
        Version = version;
        CreatedAt = createdAt;
        UpdatedAt = updatedAt;
    }

    public static CustomerRisk Rehydrate(
        CustomerRiskId id,
        Guid customerId,
        RiskLevel riskLevel,
        decimal dailyTransactionLimit,
        int dailyTransactionCountLimit,
        int transactionsToday,
        decimal amountToday,
        DateTimeOffset lastResetDate,
        int version,
        DateTimeOffset createdAt,
        DateTimeOffset updatedAt)
        => new(id, customerId, riskLevel, dailyTransactionLimit, dailyTransactionCountLimit,
               transactionsToday, amountToday, lastResetDate, version, createdAt, updatedAt);
}
