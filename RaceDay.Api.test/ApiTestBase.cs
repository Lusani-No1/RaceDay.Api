using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace RaceDay.Api.Tests;

/// <summary>Shared helpers: create clients, register/log in users, and set up events.</summary>
public abstract class ApiTestBase
{
    protected const string Password = "Password123";

    protected RaceDayFactory Factory { get; }

    protected ApiTestBase(RaceDayFactory factory) => Factory = factory;

    /// <summary>A new client has its own cookie jar, so it is its own browser session.</summary>
    protected HttpClient NewClient() => Factory.CreateClient(new WebApplicationFactoryClientOptions
    {
        BaseAddress = new Uri("https://localhost"),
        AllowAutoRedirect = false,
        HandleCookies = true
    });

    protected static string UniqueEmail() => $"{Guid.NewGuid():N}@test.com";

    protected static Task<HttpResponseMessage> RegisterAsync(
        HttpClient client, string role, string email, string password = Password, string? organiserCode = null)
        => client.PostAsJsonAsync("/api/auth/register", new
        {
            fullName = "Test User",
            email,
            password,
            role,
            organiserAccessCode = organiserCode
        });

    protected static Task<HttpResponseMessage> LoginAsync(HttpClient client, string email, string password = Password)
        => client.PostAsJsonAsync("/api/auth/login", new { email, password });

    /// <summary>Registers a new user with the given role and returns a client that is logged in as them.</summary>
    protected async Task<HttpClient> LoggedInClientAsync(string role)
    {
        var client = NewClient();
        var email = UniqueEmail();
        var code = role == "Organiser" ? RaceDayFactory.OrganiserCode : null;

        (await RegisterAsync(client, role, email, Password, code)).EnsureSuccessStatusCode();
        (await LoginAsync(client, email)).EnsureSuccessStatusCode();
        return client;
    }

    protected static async Task<JsonElement> ReadJsonAsync(HttpResponseMessage response)
        => await response.Content.ReadFromJsonAsync<JsonElement>();

    protected static object EventBody(string name = "Joburg City 10km") => new
    {
        name,
        description = "Test event",
        eventDate = "2026-11-08",
        location = "Johannesburg",
        distanceKm = 10,
        eventType = "Run"
    };

    protected static object CategoryBody(string name = "10km Open") => new
    {
        name,
        distanceKm = 10,
        price = 150
    };

    protected async Task<int> CreateEventAsync(HttpClient organiser)
    {
        var response = await organiser.PostAsJsonAsync("/api/events", EventBody());
        response.EnsureSuccessStatusCode();
        return (await ReadJsonAsync(response)).GetProperty("eventId").GetInt32();
    }

    protected async Task<int> CreateCategoryAsync(HttpClient organiser, int eventId)
    {
        var response = await organiser.PostAsJsonAsync($"/api/events/{eventId}/categories", CategoryBody());
        response.EnsureSuccessStatusCode();
        return (await ReadJsonAsync(response)).GetProperty("categoryId").GetInt32();
    }

    protected async Task<(int EventId, int CategoryId)> CreateEventWithCategoryAsync(HttpClient organiser)
    {
        var eventId = await CreateEventAsync(organiser);
        var categoryId = await CreateCategoryAsync(organiser, eventId);
        return (eventId, categoryId);
    }

    protected static Task<HttpResponseMessage> EnrolAsync(HttpClient participant, int categoryId)
        => participant.PostAsync($"/api/categories/{categoryId}/enrolments", null);

    protected static async Task<int> EnrolAndGetIdAsync(HttpClient participant, int categoryId)
    {
        var response = await EnrolAsync(participant, categoryId);
        response.EnsureSuccessStatusCode();
        return (await ReadJsonAsync(response)).GetProperty("enrolmentId").GetInt32();
    }
}
