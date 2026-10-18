using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace RaceDay.Api.Tests;

public class CategoryTests : ApiTestBase, IClassFixture<RaceDayFactory>
{
    public CategoryTests(RaceDayFactory factory) : base(factory) { }

    [Fact]
    public async Task Organiser_CanAddDistanceCategory()
    {
        var organiser = await LoggedInClientAsync("Organiser");
        var eventId = await CreateEventAsync(organiser);

        var response = await organiser.PostAsJsonAsync($"/api/events/{eventId}/categories",
            new { name = "21km", distanceKm = 21, price = 250 });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal(21, (await ReadJsonAsync(response)).GetProperty("distanceKm").GetDecimal());
    }

    [Fact]
    public async Task Organiser_CanAddAgeOnlyCategory()
    {
        var organiser = await LoggedInClientAsync("Organiser");
        var eventId = await CreateEventAsync(organiser);

        var response = await organiser.PostAsJsonAsync($"/api/events/{eventId}/categories",
            new { name = "Under 20", maxAge = 19, price = 100 });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal(19, (await ReadJsonAsync(response)).GetProperty("maxAge").GetInt32());
    }

    [Fact]
    public async Task AddCategory_WithNoDistanceOrAge_Returns400()
    {
        var organiser = await LoggedInClientAsync("Organiser");
        var eventId = await CreateEventAsync(organiser);

        var response = await organiser.PostAsJsonAsync($"/api/events/{eventId}/categories",
            new { name = "Nothing", price = 50 });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task AddCategory_WithMinAgeAboveMaxAge_Returns400()
    {
        var organiser = await LoggedInClientAsync("Organiser");
        var eventId = await CreateEventAsync(organiser);

        var response = await organiser.PostAsJsonAsync($"/api/events/{eventId}/categories",
            new { name = "Backwards", minAge = 60, maxAge = 20, price = 50 });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Participant_CannotAddCategory_Returns403()
    {
        var organiser = await LoggedInClientAsync("Organiser");
        var eventId = await CreateEventAsync(organiser);
        var participant = await LoggedInClientAsync("Participant");

        var response = await participant.PostAsJsonAsync($"/api/events/{eventId}/categories", CategoryBody());

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Organiser_CannotAddCategoryToAnotherOrganisersEvent_Returns403()
    {
        var owner = await LoggedInClientAsync("Organiser");
        var eventId = await CreateEventAsync(owner);
        var otherOrganiser = await LoggedInClientAsync("Organiser");

        var response = await otherOrganiser.PostAsJsonAsync($"/api/events/{eventId}/categories", CategoryBody());

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task AddCategory_ToMissingEvent_Returns404()
    {
        var organiser = await LoggedInClientAsync("Organiser");

        var response = await organiser.PostAsJsonAsync("/api/events/999999/categories", CategoryBody());

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task BothRoles_CanListCategories()
    {
        var organiser = await LoggedInClientAsync("Organiser");
        var (eventId, _) = await CreateEventWithCategoryAsync(organiser);
        var participant = await LoggedInClientAsync("Participant");

        var response = await participant.GetAsync($"/api/events/{eventId}/categories");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(1, (await ReadJsonAsync(response)).GetArrayLength());
    }

    [Fact]
    public async Task Organiser_CanUpdateAndDeleteOwnCategory()
    {
        var organiser = await LoggedInClientAsync("Organiser");
        var (_, categoryId) = await CreateEventWithCategoryAsync(organiser);

        var update = await organiser.PutAsJsonAsync($"/api/categories/{categoryId}",
            new { name = "Senior", minAge = 60, price = 120 });
        Assert.Equal(HttpStatusCode.OK, update.StatusCode);
        Assert.Equal("Senior", (await ReadJsonAsync(update)).GetProperty("name").GetString());

        var delete = await organiser.DeleteAsync($"/api/categories/{categoryId}");
        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);
    }

    [Fact]
    public async Task Participant_CannotUpdateOrDeleteCategory_Returns403()
    {
        var organiser = await LoggedInClientAsync("Organiser");
        var (_, categoryId) = await CreateEventWithCategoryAsync(organiser);
        var participant = await LoggedInClientAsync("Participant");

        Assert.Equal(HttpStatusCode.Forbidden,
            (await participant.PutAsJsonAsync($"/api/categories/{categoryId}", CategoryBody("Hacked"))).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden,
            (await participant.DeleteAsync($"/api/categories/{categoryId}")).StatusCode);
    }

    [Fact]
    public async Task DeleteCategory_WithEnrolments_Returns409()
    {
        var organiser = await LoggedInClientAsync("Organiser");
        var (_, categoryId) = await CreateEventWithCategoryAsync(organiser);
        var participant = await LoggedInClientAsync("Participant");
        await EnrolAsync(participant, categoryId);

        var response = await organiser.DeleteAsync($"/api/categories/{categoryId}");

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }
}
