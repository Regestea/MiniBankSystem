using System.Security.Cryptography;
using System.Text.RegularExpressions;
using MiniBank.Domain.BuildingBlocks.Exceptions;

namespace MiniBank.Domain.AccountAggregate.ValueObjects;

/// <summary>
/// AccountNumber — unique bank account identifier, e.g. IR-XXXXXXXXXX (10 digits) or 16-digit.
/// For simplicity: 16-digit numeric string, first digit not zero.
/// </summary>
public sealed partial record AccountNumber
{
    public string Value { get; }

    public AccountNumber(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new DomainValidationException(nameof(AccountNumber), "Account number cannot be empty.");

        value = value.Trim();

        if (!MyRegex().IsMatch(value))
            throw new DomainValidationException(nameof(AccountNumber), "Account number must be 16 digits, first digit non-zero.");

        Value = value;
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
    private static partial Regex MyRegex();
}
