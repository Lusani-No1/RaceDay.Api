using System.Net;
using Xunit;

namespace RaceDay.Api.Tests;

public class EnrolmentTests : ApiTestBase, IClassFixture<RaceDayFactory>
{
    public EnrolmentTests(RaceDayFactory factory) : base(factory) { }

    [Fact]
    public async Task Participant_CanEnrol_AndEnrolmentIsRecorded()
    {
        var organiser = await LoggedInClientAsync("Organiser");
        var (eventId, categoryId) = await CreateEventWithCategoryAsync(organiser);
        var participant = await LoggedInClientAsync("Participant");

        var enrol = await EnrolAsync(participant, categoryId);
        Assert.Equal(HttpStatusCode.Created, enrol.StatusCode);

        // The participant sees it in their own enrolments, linked to the right event and category
        var mine = await ReadJsonAsync(await participant.GetAsync("/api/enrolments/me"));
        Assert.Equal(1, mine.GetArrayLength());
        var item = mine[0];
        Assert.Equal(eventId, item.GetProperty("eventId").GetInt32());
        Assert.Equal(categoryId, item.GetProperty("categoryId").GetInt32());
        Assert.Equal("Confirmed", item.GetProperty("status").GetString());

        // The event's Organiser sees the same enrolment
        var forOrganiser = await ReadJsonAsync(await organiser.GetAsync($"/api/events/{eventId}/enrolments"));
        Assert.Equal(1, forOrganiser.GetArrayLength());
        Assert.Equal(item.GetProperty("participantId").GetInt32(),
            forOrganiser[0].GetProperty("participantId").GetInt32());
    }

    [Fact]
    public async Task Participant_EnrollingTwiceInSameCategory_Returns409()
    {
        var organiser = await LoggedInClientAsync("Organiser");
        var (_, categoryId) = await CreateEventWithCategoryAsync(organiser);
        var participant = await LoggedInClientAsync("Participant");

        Assert.Equal(HttpStatusCode.Created, (await EnrolAsync(participant, categoryId)).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await EnrolAsync(participant, categoryId)).StatusCode);
    }

    [Fact]
    public async Task Enrol_InMissingCategory_Returns404()
    {
        var participant = await LoggedInClientAsync("Participant");

        Assert.Equal(HttpStatusCode.NotFound, (await EnrolAsync(participant, 999999)).StatusCode);
    }

    [Fact]
    public async Task Organiser_CannotEnrol_Returns403()
    {
        var organiser = await LoggedInClientAsync("Organiser");
        var (_, categoryId) = await CreateEventWithCategoryAsync(organiser);

        Assert.Equal(HttpStatusCode.Forbidden, (await EnrolAsync(organiser, categoryId)).StatusCode);
    }

    [Fact]
    public async Task Enrol_WithoutLogin_Returns401()
    {
        var organiser = await LoggedInClientAsync("Organiser");
        var (_, categoryId) = await CreateEventWithCategoryAsync(organiser);

        Assert.Equal(HttpStatusCode.Unauthorized, (await EnrolAsync(NewClient(), categoryId)).StatusCode);
    }

    [Fact]
    public async Task EnrolmentListEndpoints_RejectTheWrongRole()
    {
        var organiser = await LoggedInClientAsync("Organiser");
        var (eventId, _) = await CreateEventWithCategoryAsync(organiser);
        var participant = await LoggedInClientAsync("Participant");

        // wrong role in each direction
        Assert.Equal(HttpStatusCode.Forbidden, (await organiser.GetAsync("/api/enrolments/me")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden,
            (await participant.GetAsync($"/api/events/{eventId}/enrolments")).StatusCode);
    }

    [Fact]
    public async Task OtherOrganiser_CannotViewEventEnrolments_Returns403()
    {
        var owner = await LoggedInClientAsync("Organiser");
        var (eventId, _) = await CreateEventWithCategoryAsync(owner);
        var otherOrganiser = await LoggedInClientAsync("Organiser");

        var response = await otherOrganiser.GetAsync($"/api/events/{eventId}/enrolments");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Participant_OnlySeesTheirOwnEnrolments()
    {
        var organiser = await LoggedInClientAsync("Organiser");
        var (_, categoryId) = await CreateEventWithCategoryAsync(organiser);
        var first = await LoggedInClientAsync("Participant");
        var second = await LoggedInClientAsync("Participant");
        await EnrolAsync(first, categoryId);

        var secondsList = await ReadJsonAsync(await second.GetAsync("/api/enrolments/me"));

        Assert.Equal(0, secondsList.GetArrayLength());
    }

    [Fact]
    public async Task Participant_CanCancelOwnEnrolment_ThenEnrolAgain()
    {
        var organiser = await LoggedInClientAsync("Organiser");
        var (_, categoryId) = await CreateEventWithCategoryAsync(organiser);
        var participant = await LoggedInClientAsync("Participant");
        var enrolmentId = await EnrolAndGetIdAsync(participant, categoryId);

        var cancel = await participant.DeleteAsync($"/api/enrolments/{enrolmentId}");
        Assert.Equal(HttpStatusCode.NoContent, cancel.StatusCode);

        var mine = await ReadJsonAsync(await participant.GetAsync("/api/enrolments/me"));
        Assert.Equal("Cancelled", mine[0].GetProperty("status").GetString());

        // enrolling again re-activates the same record
        Assert.Equal(HttpStatusCode.Created, (await EnrolAsync(participant, categoryId)).StatusCode);
        var after = await ReadJsonAsync(await participant.GetAsync("/api/enrolments/me"));
        Assert.Equal(1, after.GetArrayLength());
        Assert.Equal("Confirmed", after[0].GetProperty("status").GetString());
    }

    [Fact]
    public async Task Participant_CannotCancelSomeoneElsesEnrolment_Returns403()
    {
        var organiser = await LoggedInClientAsync("Organiser");
        var (_, categoryId) = await CreateEventWithCategoryAsync(organiser);
        var owner = await LoggedInClientAsync("Participant");
        var enrolmentId = await EnrolAndGetIdAsync(owner, categoryId);
        var intruder = await LoggedInClientAsync("Participant");

        var response = await intruder.DeleteAsync($"/api/enrolments/{enrolmentId}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task CancelEnrolment_Missing_Returns404()
    {
        var participant = await LoggedInClientAsync("Participant");

        Assert.Equal(HttpStatusCode.NotFound, (await participant.DeleteAsync("/api/enrolments/999999")).StatusCode);
    }
}
