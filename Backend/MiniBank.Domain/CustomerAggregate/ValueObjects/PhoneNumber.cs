using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;
using MiniBank.Domain.BuildingBlocks.Exceptions;

namespace MiniBank.Domain.CustomerAggregate.ValueObjects;

public record PhoneNumber
{
    private string Value { get; }

    public PhoneNumber(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new DomainValidationException(nameof(PhoneNumber), "Phone number cannot be empty.");

        if (!Regex.IsMatch(value, @"^\d+$"))
            throw new DomainValidationException(nameof(PhoneNumber), "Invalid phone number, only numbers allowed.");

        if (value.Length < 10 || value.Length > 15)
            throw new DomainValidationException(nameof(PhoneNumber), "Phone number must be between 10 and 15 digits.");

        Value = value;
    }

    public static implicit operator string(PhoneNumber phoneNumber)
        => phoneNumber.Value;

    public static implicit operator PhoneNumber(string phoneNumber)
        => new(phoneNumber);
}