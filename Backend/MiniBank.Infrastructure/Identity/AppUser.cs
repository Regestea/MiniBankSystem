using Microsoft.AspNetCore.Identity;
using MiniBank.Domain.CustomerAggregate.ValueObjects;

namespace MiniBank.Infrastructure.Identity;

/// <summary>
/// Application user backed by ASP.NET Core Identity default tables (AspNetUsers…).
/// Optionally linked to the domain Customer aggregate.
/// </summary>
public sealed class AppUser : IdentityUser
{
    /// <summary>Links this login identity to a customers row (nullable until onboarding completes).</summary>
    public CustomerId? CustomerId { get; set; }
}
