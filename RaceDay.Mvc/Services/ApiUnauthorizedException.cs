namespace RaceDay.Mvc.Services;

/// <summary>Thrown when the API rejects the stored session; MVC handles it by signing out.</summary>
public class ApiUnauthorizedException : Exception
{
    public ApiUnauthorizedException()
        : base("Your session has expired. Please sign in again.")
    {
    }
}
