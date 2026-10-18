using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace RaceDay.Api.Tests;

public class ResultTests : ApiTestBase, IClassFixture<RaceDayFactory>
{
    public ResultTests(RaceDayFactory factory) : base(factory) { }

    private static object ResultBody(string finishTime = "00:48:32", int position = 1)
        => new { finishTime, position };

    /// <summary>An Organiser with an event, plus a Participant already enrolled in it.</summary>
    private async Task<(HttpClient Organiser, HttpClient Participant, int EventId, int EnrolmentId)> SetUpAsync()
    {
        var organiser = await LoggedInClientAsync("Organiser");
        var (eventId, categoryId) = await CreateEventWithCategoryAsync(organiser);
        var participant = await LoggedInClientAsync("Participant");
        var enrolmentId = await EnrolAndGetIdAsync(participant, categoryId);
        return (organiser, participant, eventId, enrolmentId);
    }

    [Fact]
    public async Task Organiser_CanCaptureResult_AndParticipantCanSeeIt()
    {
        var (organiser, participant, _, enrolmentId) = await SetUpAsync();

        var capture = await organiser.PostAsJsonAsync($"/api/enrolments/{enrolmentId}/results", ResultBody());
        Assert.Equal(HttpStatusCode.Created, capture.StatusCode);

        var mine = await ReadJsonAsync(await participant.GetAsync("/api/results/me"));
        Assert.Equal(1, mine.GetArrayLength());
        Assert.Equal(1, mine[0].GetProperty("position").GetInt32());
        Assert.Equal("00:48:32", mine[0].GetProperty("finishTime").GetString());
    }

    [Fact]
    public async Task CaptureResult_Twice_Returns409()
    {
        var (organiser, _, _, enrolmentId) = await SetUpAsync();

        Assert.Equal(HttpStatusCode.Created,
            (await organiser.PostAsJsonAsync($"/api/enrolments/{enrolmentId}/results", ResultBody())).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict,
            (await organiser.PostAsJsonAsync($"/api/enrolments/{enrolmentId}/results", ResultBody())).StatusCode);
    }

    [Fact]
    public async Task Participant_CannotCaptureResult_Returns403()
    {
        var (_, participant, _, enrolmentId) = await SetUpAsync();

        var response = await participant.PostAsJsonAsync($"/api/enrolments/{enrolmentId}/results", ResultBody());

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task OtherOrganiser_CannotCaptureResult_Returns403()
    {
        var (_, _, _, enrolmentId) = await SetUpAsync();
        var otherOrganiser = await LoggedInClientAsync("Organiser");

        var response = await otherOrganiser.PostAsJsonAsync($"/api/enrolments/{enrolmentId}/results", ResultBody());

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task CaptureResult_WithoutLogin_Returns401()
    {
        var (_, _, _, enrolmentId) = await SetUpAsync();

        var response = await NewClient().PostAsJsonAsync($"/api/enrolments/{enrolmentId}/results", ResultBody());

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task CaptureResult_ForMissingEnrolment_Returns404()
    {
        var organiser = await LoggedInClientAsync("Organiser");

        var response = await organiser.PostAsJsonAsync("/api/enrolments/999999/results", ResultBody());

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task CaptureResult_WithPositionZero_Returns400()
    {
        var (organiser, _, _, enrolmentId) = await SetUpAsync();

        var response = await organiser.PostAsJsonAsync($"/api/enrolments/{enrolmentId}/results",
            ResultBody(position: 0));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CaptureResult_WithZeroFinishTime_Returns400()
    {
        var (organiser, _, _, enrolmentId) = await SetUpAsync();

        var response = await organiser.PostAsJsonAsync($"/api/enrolments/{enrolmentId}/results",
            ResultBody(finishTime: "00:00:00"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CaptureResult_ForCancelledEnrolment_Returns409()
    {
        var (organiser, participant, _, enrolmentId) = await SetUpAsync();
        await participant.DeleteAsync($"/api/enrolments/{enrolmentId}");

        var response = await organiser.PostAsJsonAsync($"/api/enrolments/{enrolmentId}/results", ResultBody());

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task CancelEnrolment_AfterResultCaptured_Returns409()
    {
        var (organiser, participant, _, enrolmentId) = await SetUpAsync();
        await organiser.PostAsJsonAsync($"/api/enrolments/{enrolmentId}/results", ResultBody());

        var response = await participant.DeleteAsync($"/api/enrolments/{enrolmentId}");

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Organiser_CanCorrectResult_ButParticipantCannot()
    {
        var (organiser, participant, _, enrolmentId) = await SetUpAsync();
        var capture = await organiser.PostAsJsonAsync($"/api/enrolments/{enrolmentId}/results", ResultBody());
        var resultId = (await ReadJsonAsync(capture)).GetProperty("resultId").GetInt32();

        var update = await organiser.PutAsJsonAsync($"/api/results/{resultId}", ResultBody("00:45:10", 2));
        Assert.Equal(HttpStatusCode.OK, update.StatusCode);
        var json = await ReadJsonAsync(update);
        Assert.Equal("00:45:10", json.GetProperty("finishTime").GetString());
        Assert.Equal(2, json.GetProperty("position").GetInt32());

        Assert.Equal(HttpStatusCode.Forbidden,
            (await participant.PutAsJsonAsync($"/api/results/{resultId}", ResultBody())).StatusCode);
    }

    [Fact]
    public async Task Organiser_CanViewEventResults_ButParticipantCannot()
    {
        var (organiser, participant, eventId, enrolmentId) = await SetUpAsync();
        await organiser.PostAsJsonAsync($"/api/enrolments/{enrolmentId}/results", ResultBody());

        var forOrganiser = await organiser.GetAsync($"/api/events/{eventId}/results");
        Assert.Equal(HttpStatusCode.OK, forOrganiser.StatusCode);
        Assert.Equal(1, (await ReadJsonAsync(forOrganiser)).GetArrayLength());

        Assert.Equal(HttpStatusCode.Forbidden,
            (await participant.GetAsync($"/api/events/{eventId}/results")).StatusCode);
    }

    [Fact]
    public async Task Participant_OnlySeesTheirOwnResults()
    {
        var (organiser, _, _, enrolmentId) = await SetUpAsync();
        await organiser.PostAsJsonAsync($"/api/enrolments/{enrolmentId}/results", ResultBody());
        var otherParticipant = await LoggedInClientAsync("Participant");

        var theirs = await ReadJsonAsync(await otherParticipant.GetAsync("/api/results/me"));

        Assert.Equal(0, theirs.GetArrayLength());
    }
}
