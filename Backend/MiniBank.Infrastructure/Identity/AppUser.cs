using Microsoft.AspNetCore.Identity;
using MiniBank.Domain.CustomerAggregate.ValueObjects;

namespace MiniBank.Infrastructure.Identity;

/// <summary>Identity user, optionally linked to a Customer.</summary>
public sealed class AppUser : IdentityUser
{
    public CustomerId? CustomerId { get; set; }
}
