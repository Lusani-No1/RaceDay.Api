namespace RaceDay.Mvc.Services;

/// <summary>Turns API-relative image paths such as /uploads/x.jpg into absolute URLs for the browser.</summary>
public class ApiUrl
{
    private readonly string _baseUrl;

    public ApiUrl(IConfiguration configuration)
    {
        // Inside Docker the API is reached as http://api:8080, but the browser needs a
        // publicly reachable address for uploaded images. Api:PublicBaseUrl overrides that.
        var publicBase = configuration["Api:PublicBaseUrl"];
        _baseUrl = (string.IsNullOrWhiteSpace(publicBase)
            ? configuration["Api:BaseUrl"] ?? "http://localhost:5062"
            : publicBase).TrimEnd('/');
    }

    public string Resolve(string? url)
    {
        if (string.IsNullOrWhiteSpace(url)) return string.Empty;
        if (url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
            url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            return url;

        return $"{_baseUrl}/{url.TrimStart('/')}";
    }
}
