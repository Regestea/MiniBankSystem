using System.Text.RegularExpressions;
using MiniBank.Domain.BuildingBlocks.Exceptions;

namespace MiniBank.Domain.CustomerAggregate.ValueObjects;

public sealed partial record Email
{
    public string Value { get; }

    public Email(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new DomainValidationException(nameof(Email), "Email cannot be empty.");

        value = value.Trim().ToLowerInvariant();

        // Simple RFC-ish validation
        if (!MyRegex().IsMatch(value))
            throw new DomainValidationException(nameof(Email), "Invalid email format.");

        if (value.Length > 254)
            throw new DomainValidationException(nameof(Email), "Email too long.");

        Value = value;
    }

    public static implicit operator string(Email email) => email.Value;
    public static implicit operator Email(string email) => new(email);

    public override string ToString() => Value;
    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$")]
    private static partial Regex MyRegex();
}
