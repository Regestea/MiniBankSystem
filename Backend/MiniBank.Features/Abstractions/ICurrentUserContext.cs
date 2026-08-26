namespace MiniBank.Features.Abstractions;

/// <summary>
/// Ambient information about the authenticated caller (populated by the Api host).
/// </summary>
public interface ICurrentUserContext
{
    /// <summary>Identity user id (AspNetUsers.id) — throws when unauthenticated.</summary>
    Guid UserId { get; }

    /// <summary>Linked customers.id — null when the user has not completed onboarding.</summary>
    Task<Guid?> GetCustomerIdAsync(CancellationToken cancellationToken = default);
}
