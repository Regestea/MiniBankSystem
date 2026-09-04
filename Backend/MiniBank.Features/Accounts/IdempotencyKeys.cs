using MiniBank.Domain.BuildingBlocks;
using MiniBank.Domain.BuildingBlocks.Exceptions;

namespace MiniBank.Features.Accounts;

/// <summary>
/// Shared idempotency-key handling for money operations (Deposit/Withdraw/Transfer).
/// The key is GLOBAL (ux_transactions_reference): same key + same payload replays the
/// original transaction; same key + different payload is 409 — never another tx.
/// Retry loops stay in each handler (explicit per-slice policy); only the key
/// normalization + conflict factory are shared so the three slices cannot drift.
/// </summary>
internal static class IdempotencyKeys
{
    public const int MaxRetries = 3;

    public static string Normalize(string? raw)
    {
        var key = raw?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(key))
            throw new DomainValidationException("IdempotencyKey", "IdempotencyKey must not be empty or whitespace.");
        if (key.Length > BankingRules.MaxIdempotencyKeyLength)
            throw new DomainValidationException("IdempotencyKey",
                $"IdempotencyKey must not exceed {BankingRules.MaxIdempotencyKeyLength} characters.");
        return key;
    }

    public static DomainConflictException Mismatch(string? details = null)
        => new("IdempotencyKey",
            details ?? "Idempotency key was already used with a different amount, type or account.");

    public static DomainConflictException RetryExhausted()
        => new("Concurrency",
            "The operation could not be completed due to concurrent modifications. Please retry.");
}
