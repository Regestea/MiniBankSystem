using System.Text.RegularExpressions;
using MiniBank.Domain.BuildingBlocks.Exceptions;

namespace MiniBank.Domain.CustomerAggregate.ValueObjects;

public record FullName
{
    private string Value { get; }

    public FullName(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new DomainValidationException(nameof(FullName), "Full name cannot be empty.");

        // International letters (Unicode), spaces, apostrophes, periods, hyphens — e.g. Jean-Luc, O'Brien, علی
        if (!Regex.IsMatch(value, @"^[\p{L}\s'.-]{2,100}$"))
            throw new DomainValidationException(nameof(FullName), "Invalid full name format.");

        Value = value.Trim();
    }

    public static implicit operator string(FullName fullName)
        => fullName.Value;

    public static implicit operator FullName(string fullName)
        => new(fullName);
}