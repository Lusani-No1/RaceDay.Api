using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace RaceDay.Api.Tests;

public class EventTests : ApiTestBase, IClassFixture<RaceDayFactory>
{
    public EventTests(RaceDayFactory factory) : base(factory) { }

    // ---------- Organisers can manage events ----------

    [Fact]
    public async Task Organiser_CanCreateEvent_AndFetchIt()
    {
        var organiser = await LoggedInClientAsync("Organiser");

        var create = await organiser.PostAsJsonAsync("/api/events", EventBody("Soweto Marathon"));
        Assert.Equal(HttpStatusCode.Created, create.StatusCode);

        var created = await ReadJsonAsync(create);
        Assert.Equal("Run", created.GetProperty("eventType").GetString());
        Assert.Equal(10, created.GetProperty("distanceKm").GetDecimal());

        var get = await organiser.GetAsync($"/api/events/{created.GetProperty("eventId").GetInt32()}");
        Assert.Equal(HttpStatusCode.OK, get.StatusCode);
        Assert.Equal("Soweto Marathon", (await ReadJsonAsync(get)).GetProperty("name").GetString());
    }

    [Fact]
    public async Task Organiser_CanUpdateOwnEvent()
    {
        var organiser = await LoggedInClientAsync("Organiser");
        var eventId = await CreateEventAsync(organiser);

        var update = await organiser.PutAsJsonAsync($"/api/events/{eventId}", new
        {
            name = "Renamed Event",
            description = "Changed",
            eventDate = "2026-12-01",
            location = "Pretoria",
            distanceKm = 21,
            eventType = "Walk"
        });

        Assert.Equal(HttpStatusCode.OK, update.StatusCode);
        var json = await ReadJsonAsync(update);
        Assert.Equal("Renamed Event", json.GetProperty("name").GetString());
        Assert.Equal("Walk", json.GetProperty("eventType").GetString());
    }

    [Fact]
    public async Task Organiser_CanDeleteOwnEvent()
    {
        var organiser = await LoggedInClientAsync("Organiser");
        var eventId = await CreateEventAsync(organiser);

        var delete = await organiser.DeleteAsync($"/api/events/{eventId}");
        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);

        Assert.Equal(HttpStatusCode.NotFound, (await organiser.GetAsync($"/api/events/{eventId}")).StatusCode);
    }

    [Fact]
    public async Task UpdateOrDelete_MissingEvent_Returns404()
    {
        var organiser = await LoggedInClientAsync("Organiser");

        Assert.Equal(HttpStatusCode.NotFound,
            (await organiser.PutAsJsonAsync("/api/events/999999", EventBody())).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound,
            (await organiser.DeleteAsync("/api/events/999999")).StatusCode);
    }

    [Fact]
    public async Task CreateEvent_WithInvalidEventType_Returns400()
    {
        var organiser = await LoggedInClientAsync("Organiser");

        var response = await organiser.PostAsJsonAsync("/api/events", new
        {
            name = "Bad Type",
            eventDate = "2026-11-08",
            location = "Johannesburg",
            distanceKm = 10,
            eventType = "Swim"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateEvent_WithZeroDistance_Returns400()
    {
        var organiser = await LoggedInClientAsync("Organiser");

        var response = await organiser.PostAsJsonAsync("/api/events", new
        {
            name = "No Distance",
            eventDate = "2026-11-08",
            location = "Johannesburg",
            distanceKm = 0,
            eventType = "Run"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateEvent_WithoutName_Returns400()
    {
        var organiser = await LoggedInClientAsync("Organiser");

        var response = await organiser.PostAsJsonAsync("/api/events", new
        {
            eventDate = "2026-11-08",
            location = "Johannesburg",
            distanceKm = 10,
            eventType = "Run"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // ---------- Wrong roles and wrong owners are rejected ----------

    [Fact]
    public async Task Participant_CannotCreateEvent_Returns403()
    {
        var participant = await LoggedInClientAsync("Participant");

        var response = await participant.PostAsJsonAsync("/api/events", EventBody());

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Participant_CannotUpdateOrDeleteEvent_Returns403()
    {
        var organiser = await LoggedInClientAsync("Organiser");
        var eventId = await CreateEventAsync(organiser);
        var participant = await LoggedInClientAsync("Participant");

        Assert.Equal(HttpStatusCode.Forbidden,
            (await participant.PutAsJsonAsync($"/api/events/{eventId}", EventBody("Hacked"))).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden,
            (await participant.DeleteAsync($"/api/events/{eventId}")).StatusCode);
    }

    [Fact]
    public async Task Organiser_CannotChangeAnotherOrganisersEvent_Returns403()
    {
        var owner = await LoggedInClientAsync("Organiser");
        var eventId = await CreateEventAsync(owner);
        var otherOrganiser = await LoggedInClientAsync("Organiser");

        Assert.Equal(HttpStatusCode.Forbidden,
            (await otherOrganiser.PutAsJsonAsync($"/api/events/{eventId}", EventBody("Hacked"))).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden,
            (await otherOrganiser.DeleteAsync($"/api/events/{eventId}")).StatusCode);

        // the event is untouched
        var get = await owner.GetAsync($"/api/events/{eventId}");
        Assert.Equal("Joburg City 10km", (await ReadJsonAsync(get)).GetProperty("name").GetString());
    }

    [Fact]
    public async Task CreateEvent_WithoutLogin_Returns401()
    {
        var response = await NewClient().PostAsJsonAsync("/api/events", EventBody());

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // ---------- Both roles can view ----------

    [Fact]
    public async Task Participant_CanViewEventsAndEventDetails()
    {
        var organiser = await LoggedInClientAsync("Organiser");
        var (eventId, _) = await CreateEventWithCategoryAsync(organiser);
        var participant = await LoggedInClientAsync("Participant");

        var list = await participant.GetAsync("/api/events");
        Assert.Equal(HttpStatusCode.OK, list.StatusCode);
        Assert.True((await ReadJsonAsync(list)).GetArrayLength() >= 1);

        var detail = await participant.GetAsync($"/api/events/{eventId}");
        Assert.Equal(HttpStatusCode.OK, detail.StatusCode);
        Assert.Equal(1, (await ReadJsonAsync(detail)).GetProperty("categories").GetArrayLength());
    }

    [Fact]
    public async Task GetEvent_Missing_Returns404()
    {
        var participant = await LoggedInClientAsync("Participant");

        Assert.Equal(HttpStatusCode.NotFound, (await participant.GetAsync("/api/events/999999")).StatusCode);
    }

    // ---------- Protected deletes ----------

    [Fact]
    public async Task DeleteEvent_WithEnrolments_Returns409()
    {
        var organiser = await LoggedInClientAsync("Organiser");
        var (eventId, categoryId) = await CreateEventWithCategoryAsync(organiser);
        var participant = await LoggedInClientAsync("Participant");
        await EnrolAsync(participant, categoryId);

        var response = await organiser.DeleteAsync($"/api/events/{eventId}");

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }
}
