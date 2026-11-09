using System.Security.Claims;

namespace RaceDay.Mvc.Extensions;

public static class ClaimsPrincipalExtensions
{
    public static int GetUserId(this ClaimsPrincipal principal)
        => int.Parse(principal.FindFirstValue(ClaimTypes.NameIdentifier)!);

    public static string GetFullName(this ClaimsPrincipal principal)
        => principal.FindFirstValue(ClaimTypes.Name) ?? "Account";

    public static string GetRole(this ClaimsPrincipal principal)
        => principal.FindFirstValue(ClaimTypes.Role) ?? string.Empty;
}
