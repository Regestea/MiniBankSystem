namespace MiniBank.Abstractions;

/// <summary>
/// Provides current caller identity from the auth token.
/// The identity user id doubles as the customer id (1:1 same-Guid design),
/// so no link table, claim lookup or extra DB round-trip is needed.
/// </summary>
public interface ICurrentUserContext
{
    Guid UserId { get; }

    /// <summary>Caller email from token claims, if present.</summary>
    string? Email { get; }

    /// <summary>True when the caller carries the "Admin" role claim. Lets handlers bypass ownership checks.</summary>
    bool IsAdmin { get; }
}
