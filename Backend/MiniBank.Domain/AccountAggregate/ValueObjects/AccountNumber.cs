using System.Text.RegularExpressions;
using MiniBank.Domain.BuildingBlocks.Exceptions;

namespace MiniBank.Domain.AccountAggregate.ValueObjects;

/// <summary>
/// AccountNumber — unique bank account identifier, e.g. IR-XXXXXXXXXX (10 digits) or 16-digit.
/// For simplicity: 16-digit numeric string, first digit not zero.
/// </summary>
public sealed record AccountNumber
{
    public string Value { get; }

    public AccountNumber(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new DomainValidationException(nameof(AccountNumber), "Account number cannot be empty.");

        value = value.Trim();

        if (!Regex.IsMatch(value, @"^[1-9]\d{15}$"))
            throw new DomainValidationException(nameof(AccountNumber), "Account number must be 16 digits, first digit non-zero.");

        Value = value;
    }

    public static AccountNumber Generate()
    {
        // Generate 16-digit number, first digit 1-9
        var rng = Random.Shared;
        var digits = new char[16];
        digits[0] = (char)('1' + rng.Next(9));
        for (int i = 1; i < 16; i++)
            digits[i] = (char)('0' + rng.Next(10));
        return new AccountNumber(new string(digits));
    }

    public static implicit operator string(AccountNumber number) => number.Value;
    public static implicit operator AccountNumber(string value) => new(value);

    public override string ToString() => Value;
}
