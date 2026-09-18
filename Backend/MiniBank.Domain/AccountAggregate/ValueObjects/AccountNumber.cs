using System.Security.Cryptography;
using System.Text.RegularExpressions;
using MiniBank.Domain.BuildingBlocks.Exceptions;

namespace MiniBank.Domain.AccountAggregate.ValueObjects;

/// <summary>
/// AccountNumber — unique bank account identifier.
/// Accepted formats (case-insensitive, trimmed):
///   - IR-XXXXXXXXXX (IR- + 10 digits, e.g. IR-1234567890)
///   - 16-digit numeric string, first digit non-zero (e.g. 1234567890123456)
/// Stored normalized: trimmed + upper-cased (so "ir-123..." becomes "IR-123...").
/// Amounts everywhere are USD (see Money).
/// </summary>
public sealed partial record AccountNumber
{
    public string Value { get; }

    public AccountNumber(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new DomainValidationException(nameof(AccountNumber), "Account number cannot be empty.");

        value = value.Trim().ToUpperInvariant();

        if (!SixteenDigit().IsMatch(value) && !IrFormat().IsMatch(value))
            throw new DomainValidationException(nameof(AccountNumber), "Account number must be IR-XXXXXXXXXX (10 digits) or 16 digits, first digit non-zero.");

        Value = value;
    }

    /// <summary>Normalizes raw user input (trim + upper-case) without validating.</summary>
    public static string Normalize(string? value) => (value ?? string.Empty).Trim().ToUpperInvariant();

    /// <summary>True when the raw input is a syntactically valid account number (either format).</summary>
    public static bool IsValid(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return false;
        var normalized = Normalize(value);
        return SixteenDigit().IsMatch(normalized) || IrFormat().IsMatch(normalized);
    }

    public static AccountNumber Generate()
    {
        // Use cryptographic RNG for unpredictable account numbers
        var bytes = RandomNumberGenerator.GetBytes(8);
        var number = BitConverter.ToUInt64(bytes);

        // 16 digits, first digit 1-9
        var firstDigit = (char)('1' + (int)(number % 9));
        var remaining = (number % 1_000_000_000_000_000).ToString("D15");

        return new AccountNumber($"{firstDigit}{remaining[..15]}");
    }

    public static implicit operator string(AccountNumber number) => number.Value;
    public static implicit operator AccountNumber(string value) => new(value);

    public override string ToString() => Value;
    [GeneratedRegex(@"^[1-9]\d{15}$")]
    private static partial Regex SixteenDigit();
    [GeneratedRegex(@"^IR-\d{10}$")]
    private static partial Regex IrFormat();
}
