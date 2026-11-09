using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using RaceDay.Mvc.Models;

namespace RaceDay.Mvc.Services;

/// <summary>
/// All communication with the Part 2 API goes through this client. The API's session
/// cookie is kept server-side in MVC session and attached to every request.
/// </summary>
public class RaceDayApiClient
{
    private const string ApiCookieName = "RaceDay.Session";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly HttpClient _http;
    private readonly IHttpContextAccessor _httpContext;

    public RaceDayApiClient(HttpClient http, IHttpContextAccessor httpContext)
    {
        _http = http;
        _httpContext = httpContext;
    }

    // ---------- Auth ----------

    public Task<ApiResult<UserDto>> RegisterAsync(RegisterRequest request)
        => SendAsync<UserDto>(HttpMethod.Post, "/api/auth/register", request);

    public Task<ApiResult<UserDto>> LoginAsync(LoginRequest request)
        => SendAsync<UserDto>(HttpMethod.Post, "/api/auth/login", request, throwOnUnauthorized: false);

    public async Task<ApiResult> LogoutAsync()
    {
        var result = await SendAsync(HttpMethod.Post, "/api/auth/logout", null, throwOnUnauthorized: false);
        _httpContext.HttpContext?.Session.Remove(SessionKeys.ApiCookie);
        return result;
    }

    // ---------- Profile ----------

    public Task<ApiResult<ProfileDto>> GetProfileAsync()
        => SendAsync<ProfileDto>(HttpMethod.Get, "/api/users/me");

    public Task<ApiResult<ProfileDto>> UpdateProfileAsync(UpdateProfileRequest request)
        => SendAsync<ProfileDto>(HttpMethod.Put, "/api/users/me", request);

    public Task<ApiResult<ProfileDto>> UploadProfilePictureAsync(IFormFile file)
        => UploadAsync<ProfileDto>("/api/users/me/profile-picture", file);

    // ---------- Events ----------

    public Task<ApiResult<List<EventDto>>> GetEventsAsync()
        => SendAsync<List<EventDto>>(HttpMethod.Get, "/api/events");

    public Task<ApiResult<EventDetailDto>> GetEventAsync(int eventId)
        => SendAsync<EventDetailDto>(HttpMethod.Get, $"/api/events/{eventId}");

    public Task<ApiResult<EventDto>> CreateEventAsync(EventRequest request)
        => SendAsync<EventDto>(HttpMethod.Post, "/api/events", request);

    public Task<ApiResult<EventDto>> UpdateEventAsync(int eventId, EventRequest request)
        => SendAsync<EventDto>(HttpMethod.Put, $"/api/events/{eventId}", request);

    public Task<ApiResult> DeleteEventAsync(int eventId)
        => SendAsync(HttpMethod.Delete, $"/api/events/{eventId}");

    public Task<ApiResult<EventDto>> UploadBannerAsync(int eventId, IFormFile file)
        => UploadAsync<EventDto>($"/api/events/{eventId}/banner", file);

    // ---------- Categories ----------

    public Task<ApiResult<List<CategoryDto>>> GetCategoriesAsync(int eventId)
        => SendAsync<List<CategoryDto>>(HttpMethod.Get, $"/api/events/{eventId}/categories");

    public Task<ApiResult<CategoryDto>> CreateCategoryAsync(int eventId, CategoryRequest request)
        => SendAsync<CategoryDto>(HttpMethod.Post, $"/api/events/{eventId}/categories", request);

    public Task<ApiResult<CategoryDto>> UpdateCategoryAsync(int categoryId, CategoryRequest request)
        => SendAsync<CategoryDto>(HttpMethod.Put, $"/api/categories/{categoryId}", request);

    public Task<ApiResult> DeleteCategoryAsync(int categoryId)
        => SendAsync(HttpMethod.Delete, $"/api/categories/{categoryId}");

    // ---------- Enrolments ----------

    public Task<ApiResult<EnrolmentDto>> EnrolAsync(int categoryId)
        => SendAsync<EnrolmentDto>(HttpMethod.Post, $"/api/categories/{categoryId}/enrolments");

    public Task<ApiResult<List<EnrolmentDto>>> GetMyEnrolmentsAsync()
        => SendAsync<List<EnrolmentDto>>(HttpMethod.Get, "/api/enrolments/me");

    public Task<ApiResult<List<EnrolmentDto>>> GetEventEnrolmentsAsync(int eventId)
        => SendAsync<List<EnrolmentDto>>(HttpMethod.Get, $"/api/events/{eventId}/enrolments");

    public Task<ApiResult> CancelEnrolmentAsync(int enrolmentId)
        => SendAsync(HttpMethod.Delete, $"/api/enrolments/{enrolmentId}");

    // ---------- Results ----------

    public Task<ApiResult<ResultDto>> CaptureResultAsync(int enrolmentId, ResultRequest request)
        => SendAsync<ResultDto>(HttpMethod.Post, $"/api/enrolments/{enrolmentId}/results", request);

    public Task<ApiResult<ResultDto>> UpdateResultAsync(int resultId, ResultRequest request)
        => SendAsync<ResultDto>(HttpMethod.Put, $"/api/results/{resultId}", request);

    public Task<ApiResult<List<ResultDto>>> GetMyResultsAsync()
        => SendAsync<List<ResultDto>>(HttpMethod.Get, "/api/results/me");

    public Task<ApiResult<List<ResultDto>>> GetEventResultsAsync(int eventId)
        => SendAsync<List<ResultDto>>(HttpMethod.Get, $"/api/events/{eventId}/results");

    // ---------- Plumbing ----------

    private HttpRequestMessage CreateRequest(HttpMethod method, string path)
    {
        var request = new HttpRequestMessage(method, path);

        var cookie = _httpContext.HttpContext?.Session.GetString(SessionKeys.ApiCookie);
        if (!string.IsNullOrWhiteSpace(cookie))
            request.Headers.TryAddWithoutValidation("Cookie", cookie);

        return request;
    }

    private async Task CaptureSessionCookieAsync(HttpResponseMessage response)
    {
        var session = _httpContext.HttpContext?.Session;
        if (session is null) return;

        if (!response.Headers.TryGetValues("Set-Cookie", out var values)) return;

        foreach (var value in values)
        {
            if (!value.StartsWith($"{ApiCookieName}=", StringComparison.OrdinalIgnoreCase)) continue;

            var pair = value.Split(';', 2)[0];
            if (pair.Length > ApiCookieName.Length + 1)
                session.SetString(SessionKeys.ApiCookie, pair);
            break;
        }
    }

    private async Task<ApiResult<T>> SendAsync<T>(HttpMethod method, string path, object? body = null,
        bool throwOnUnauthorized = true, CancellationToken ct = default)
    {
        using var request = CreateRequest(method, path);
        if (body is not null)
            request.Content = JsonContent.Create(body, options: JsonOptions);

        using var response = await _http.SendAsync(request, ct);
        await CaptureSessionCookieAsync(response);

        if (response.StatusCode == HttpStatusCode.Unauthorized && throwOnUnauthorized)
            throw new ApiUnauthorizedException();

        if (!response.IsSuccessStatusCode)
            return ApiResult<T>.Fail((int)response.StatusCode, await ReadErrorAsync(response));

        if (response.StatusCode == HttpStatusCode.NoContent || response.Content.Headers.ContentLength == 0)
            return ApiResult<T>.Ok(default!, (int)response.StatusCode);

        var value = await response.Content.ReadFromJsonAsync<T>(JsonOptions, ct);
        return ApiResult<T>.Ok(value!, (int)response.StatusCode);
    }

    private async Task<ApiResult> SendAsync(HttpMethod method, string path, object? body = null,
        bool throwOnUnauthorized = true, CancellationToken ct = default)
    {
        using var request = CreateRequest(method, path);
        if (body is not null)
            request.Content = JsonContent.Create(body, options: JsonOptions);

        using var response = await _http.SendAsync(request, ct);
        await CaptureSessionCookieAsync(response);

        if (response.StatusCode == HttpStatusCode.Unauthorized && throwOnUnauthorized)
            throw new ApiUnauthorizedException();

        return response.IsSuccessStatusCode
            ? ApiResult.Ok((int)response.StatusCode)
            : ApiResult.Fail((int)response.StatusCode, await ReadErrorAsync(response));
    }

    private async Task<ApiResult<T>> UploadAsync<T>(string path, IFormFile file, CancellationToken ct = default)
    {
        using var request = CreateRequest(HttpMethod.Post, path);
        using var content = new MultipartFormDataContent();

        var stream = file.OpenReadStream();
        var fileContent = new StreamContent(stream);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue(
            string.IsNullOrWhiteSpace(file.ContentType) ? "application/octet-stream" : file.ContentType);
        content.Add(fileContent, "file", file.FileName);
        request.Content = content;

        using var response = await _http.SendAsync(request, ct);
        await CaptureSessionCookieAsync(response);

        if (response.StatusCode == HttpStatusCode.Unauthorized)
            throw new ApiUnauthorizedException();

        if (!response.IsSuccessStatusCode)
            return ApiResult<T>.Fail((int)response.StatusCode, await ReadErrorAsync(response));

        var value = await response.Content.ReadFromJsonAsync<T>(JsonOptions, ct);
        return ApiResult<T>.Ok(value!, (int)response.StatusCode);
    }

    private static async Task<string> ReadErrorAsync(HttpResponseMessage response)
    {
        var fallback = $"The server returned {(int)response.StatusCode}.";

        try
        {
            var json = await response.Content.ReadAsStringAsync();
            if (string.IsNullOrWhiteSpace(json)) return fallback;

            using var document = JsonDocument.Parse(json);
            var root = document.RootElement;

            if (root.TryGetProperty("errors", out var errors) && errors.ValueKind == JsonValueKind.Object)
            {
                foreach (var property in errors.EnumerateObject())
                {
                    if (property.Value.ValueKind == JsonValueKind.Array &&
                        property.Value.GetArrayLength() > 0)
                        return property.Value[0].GetString() ?? fallback;
                }
            }

            if (root.TryGetProperty("title", out var title) &&
                title.ValueKind == JsonValueKind.String &&
                !string.IsNullOrWhiteSpace(title.GetString()))
                return title.GetString()!;

            if (root.TryGetProperty("detail", out var detail) &&
                detail.ValueKind == JsonValueKind.String &&
                !string.IsNullOrWhiteSpace(detail.GetString()))
                return detail.GetString()!;
        }
        catch (JsonException)
        {
            // Not a ProblemDetails response; use the generic message.
        }

        return fallback;
    }
}
