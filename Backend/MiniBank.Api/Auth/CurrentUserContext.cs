using System.Security.Claims;
using MiniBank.Abstractions;

namespace MiniBank.Api.Auth;

/// <summary>Current user context resolved purely from token claims — no DB access.</summary>
internal sealed class CurrentUserContext(IHttpContextAccessor httpContextAccessor) : ICurrentUserContext
{
    private Guid? _userId;
    private string? _email;

    public Guid UserId
    {
        get
        {
            if (_userId.HasValue)
                return _userId.Value;

            var principal = httpContextAccessor.HttpContext?.User
                ?? throw new UnauthorizedAccessException("No HTTP context.");

            var sub = principal.FindFirstValue(ClaimTypes.NameIdentifier)
                      ?? principal.FindFirstValue("sub")
                      ?? throw new UnauthorizedAccessException("User is not authenticated.");

            _userId = Guid.Parse(sub);
            return _userId.Value;
        }
    }

    public string? Email
    {
        get
        {
            if (_email is not null)
                return _email;

            var principal = httpContextAccessor.HttpContext?.User
                ?? throw new UnauthorizedAccessException("No HTTP context.");

            _email = principal.FindFirstValue(ClaimTypes.Email)
                     ?? principal.FindFirstValue("email");
            return _email;
        }
    }
}
