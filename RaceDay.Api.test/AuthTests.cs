using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using RaceDay.Api.Data;
using Xunit;

namespace RaceDay.Api.Tests;

public class AuthTests : ApiTestBase, IClassFixture<RaceDayFactory>
{
    public AuthTests(RaceDayFactory factory) : base(factory) { }

    // ---------- Registration ----------

    [Fact]
    public async Task Register_Participant_Returns201WithRole()
    {
        var response = await RegisterAsync(NewClient(), "Participant", UniqueEmail());

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var json = await ReadJsonAsync(response);
        Assert.Equal("Participant", json.GetProperty("role").GetString());
    }

    [Fact]
    public async Task Register_Organiser_WithValidAccessCode_Returns201()
    {
        var response = await RegisterAsync(NewClient(), "Organiser", UniqueEmail(),
            organiserCode: RaceDayFactory.OrganiserCode);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var json = await ReadJsonAsync(response);
        Assert.Equal("Organiser", json.GetProperty("role").GetString());
    }

    [Fact]
    public async Task Register_Organiser_WithoutAccessCode_Returns403()
    {
        var response = await RegisterAsync(NewClient(), "Organiser", UniqueEmail());

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Register_Organiser_WithWrongAccessCode_Returns403()
    {
        var response = await RegisterAsync(NewClient(), "Organiser", UniqueEmail(), organiserCode: "WRONG-CODE");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Register_DuplicateEmail_Returns409_EvenWithDifferentCase()
    {
        var client = NewClient();
        var email = UniqueEmail();

        Assert.Equal(HttpStatusCode.Created, (await RegisterAsync(client, "Participant", email)).StatusCode);
        var again = await RegisterAsync(client, "Participant", email.ToUpperInvariant());

        Assert.Equal(HttpStatusCode.Conflict, again.StatusCode);
    }

    [Theory]
    [InlineData("not-an-email", "Password123")]
    [InlineData("valid@test.com", "short")]
    public async Task Register_InvalidEmailOrShortPassword_Returns400(string email, string password)
    {
        var response = await RegisterAsync(NewClient(), "Participant", email, password);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Register_UnknownRole_Returns400()
    {
        var response = await RegisterAsync(NewClient(), "Admin", UniqueEmail());

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Register_StoresHashedPassword_NotPlainText()
    {
        var email = UniqueEmail();
        await RegisterAsync(NewClient(), "Participant", email);

        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var user = db.Users.Single(u => u.Email == email);

        Assert.False(string.IsNullOrWhiteSpace(user.PasswordHash));
        Assert.NotEqual(Password, user.PasswordHash);
        Assert.DoesNotContain(Password, user.PasswordHash);
    }

    // ---------- Login / session ----------

    [Fact]
    public async Task Login_WithValidCredentials_Returns200_AndSessionAllowsProtectedCalls()
    {
        var client = NewClient();
        var email = UniqueEmail();
        await RegisterAsync(client, "Participant", email);

        var login = await LoginAsync(client, email);
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);

        var me = await client.GetAsync("/api/users/me");
        Assert.Equal(HttpStatusCode.OK, me.StatusCode);
        Assert.Equal(email, (await ReadJsonAsync(me)).GetProperty("email").GetString());
    }

    [Fact]
    public async Task Login_WithWrongPassword_Returns401()
    {
        var client = NewClient();
        var email = UniqueEmail();
        await RegisterAsync(client, "Participant", email);

        var response = await LoginAsync(client, email, "WrongPassword1");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_WithUnknownEmail_Returns401()
    {
        var response = await LoginAsync(NewClient(), UniqueEmail());

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_WithMissingFields_Returns400()
    {
        var response = await NewClient().PostAsJsonAsync("/api/auth/login", new { });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Theory]
    [InlineData("/api/users/me")]
    [InlineData("/api/events")]
    [InlineData("/api/enrolments/me")]
    [InlineData("/api/results/me")]
    public async Task ProtectedEndpoints_WithoutLogin_Return401(string url)
    {
        var response = await NewClient().GetAsync(url);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Logout_EndsTheSession()
    {
        var client = await LoggedInClientAsync("Participant");
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/users/me")).StatusCode);

        var logout = await client.PostAsync("/api/auth/logout", null);
        Assert.Equal(HttpStatusCode.NoContent, logout.StatusCode);

        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/users/me")).StatusCode);
    }

    [Fact]
    public async Task Logout_WithoutSession_Returns401()
    {
        var response = await NewClient().PostAsync("/api/auth/logout", null);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // ---------- User profile ----------

    [Fact]
    public async Task UpdateProfile_WhenLoggedIn_SavesChanges()
    {
        var client = await LoggedInClientAsync("Participant");

        var update = await client.PutAsJsonAsync("/api/users/me", new
        {
            fullName = "Updated Name",
            phoneNumber = "0821234567",
            dateOfBirth = "1995-03-14",
            emergencyContact = "Nomsa Nkosi - 0837654321"
        });
        Assert.Equal(HttpStatusCode.OK, update.StatusCode);

        var me = await ReadJsonAsync(await client.GetAsync("/api/users/me"));
        Assert.Equal("Updated Name", me.GetProperty("fullName").GetString());
        Assert.Equal("0821234567", me.GetProperty("phoneNumber").GetString());
    }

    [Fact]
    public async Task UpdateProfile_WithFutureDateOfBirth_Returns400()
    {
        var client = await LoggedInClientAsync("Participant");

        var response = await client.PutAsJsonAsync("/api/users/me", new
        {
            fullName = "Test User",
            dateOfBirth = "2999-01-01"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UpdateProfile_WithoutLogin_Returns401()
    {
        var response = await NewClient().PutAsJsonAsync("/api/users/me", new { fullName = "Nobody" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
