namespace MiniBank.Domain.BuildingBlocks;

/// <summary>Shared banking limits — single source of truth for validators and domain rules.</summary>
public static class BankingRules
{
    public const decimal MaxTransactionAmount = 9_999_999_999.99m;
    public const int MaxIdempotencyKeyLength = 64;
    public const int MaxPageSize = 100;
    public const int DefaultPageSize = 20;
}
