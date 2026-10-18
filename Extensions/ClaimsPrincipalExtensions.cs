using System.Security.Claims;

namespace RaceDay.Api.Extensions;

public static class ClaimsPrincipalExtensions
{
    /// <summary>Reads the logged-in user's id from the session cookie's claims.</summary>
    public static int GetUserId(this ClaimsPrincipal principal)
        => int.Parse(principal.FindFirstValue(ClaimTypes.NameIdentifier)!);
}
