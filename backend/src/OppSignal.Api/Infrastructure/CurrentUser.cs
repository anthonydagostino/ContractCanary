using System.Security.Claims;
using OppSignal.Application.Common;

namespace OppSignal.Api.Infrastructure;

public static class CurrentUser
{
    /// <summary>Resolve the authenticated user's id from the JWT subject claim.</summary>
    public static Guid GetUserId(this ClaimsPrincipal principal)
    {
        var sub = principal.FindFirstValue(ClaimTypes.NameIdentifier)
                  ?? principal.FindFirstValue("sub");
        return Guid.TryParse(sub, out var id)
            ? id
            : throw new UnauthorizedAppException("Missing or invalid subject claim.");
    }
}
