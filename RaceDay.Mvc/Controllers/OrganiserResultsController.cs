using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RaceDay.Mvc.Extensions;
using RaceDay.Mvc.Models;
using RaceDay.Mvc.Services;
using RaceDay.Mvc.ViewModels;

namespace RaceDay.Mvc.Controllers;

[Authorize(Roles = "Organiser")]
[Route("organiser/events/{eventId:int}/results")]
public class OrganiserResultsController : Controller
{
    private readonly RaceDayApiClient _api;

    public OrganiserResultsController(RaceDayApiClient api) => _api = api;

    [HttpGet("")]
    public async Task<IActionResult> Index(int eventId)
    {
        var ev = await GetOwnedEventAsync(eventId);
        if (ev is null) return NotFound();

        var results = await _api.GetEventResultsAsync(eventId);
        var enrolments = await _api.GetEventEnrolmentsAsync(eventId);

        if (!results.Success)
            TempData["Error"] = results.Error ?? "Could not load results.";
        if (!enrolments.Success)
            TempData["Error"] = enrolments.Error ?? "Could not load enrolments.";

        var allEnrolments = enrolments.Value ?? new List<EnrolmentDto>();
        var captured = (results.Value ?? new List<ResultDto>())
            .Select(r => r.EnrolmentId)
            .ToHashSet();

        return View(new EventResultsViewModel
        {
            Event = ev,
            Results = results.Value ?? new List<ResultDto>(),
            AwaitingResults = allEnrolments
                .Where(e => e.Status == "Confirmed" && !captured.Contains(e.EnrolmentId))
                .ToList()
        });
    }

    [HttpGet("capture/{enrolmentId:int}")]
    public async Task<IActionResult> Capture(int eventId, int enrolmentId)
    {
        var ev = await GetOwnedEventAsync(eventId);
        if (ev is null) return NotFound();

        var enrolments = await _api.GetEventEnrolmentsAsync(eventId);
        var enrolment = enrolments.Value?.FirstOrDefault(e =>
            e.EnrolmentId == enrolmentId && e.Status == "Confirmed");
        if (enrolment is null) return NotFound();

        return View(new ResultFormViewModel
        {
            EventId = eventId,
            EventName = ev.Name,
            EnrolmentId = enrolmentId,
            ParticipantName = enrolment.ParticipantName,
            CategoryName = enrolment.CategoryName
        });
    }

    [HttpPost("capture/{enrolmentId:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Capture(int eventId, int enrolmentId, ResultFormViewModel model)
    {
        var ev = await GetOwnedEventAsync(eventId);
        if (ev is null) return NotFound();

        model.EventId = eventId;
        model.EventName = ev.Name;
        model.EnrolmentId = enrolmentId;

        if (!ModelState.IsValid) return View(model);

        var result = await _api.CaptureResultAsync(enrolmentId,
            new ResultRequest(model.FinishTime!.Value, model.Position!.Value));

        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Error ?? "Could not capture the result.");
            return View(model);
        }

        TempData["Success"] = "Result captured.";
        return RedirectToAction(nameof(Index), new { eventId });
    }

    [HttpGet("edit/{resultId:int}")]
    public async Task<IActionResult> Edit(int eventId, int resultId)
    {
        var ev = await GetOwnedEventAsync(eventId);
        if (ev is null) return NotFound();

        var results = await _api.GetEventResultsAsync(eventId);
        var result = results.Value?.FirstOrDefault(r => r.ResultId == resultId);
        if (result is null) return NotFound();

        return View(new ResultFormViewModel
        {
            EventId = eventId,
            EventName = ev.Name,
            EnrolmentId = result.EnrolmentId,
            ParticipantName = result.ParticipantName,
            CategoryName = result.CategoryName,
            ResultId = result.ResultId,
            FinishTime = result.FinishTime,
            Position = result.Position
        });
    }

    [HttpPost("edit/{resultId:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int eventId, int resultId, ResultFormViewModel model)
    {
        var ev = await GetOwnedEventAsync(eventId);
        if (ev is null) return NotFound();

        model.EventId = eventId;
        model.EventName = ev.Name;
        model.ResultId = resultId;

        if (!ModelState.IsValid) return View(model);

        var result = await _api.UpdateResultAsync(resultId,
            new ResultRequest(model.FinishTime!.Value, model.Position!.Value));

        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Error ?? "Could not update the result.");
            return View(model);
        }

        TempData["Success"] = "Result updated.";
        return RedirectToAction(nameof(Index), new { eventId });
    }

    private async Task<EventDto?> GetOwnedEventAsync(int eventId)
    {
        var result = await _api.GetEventAsync(eventId);
        if (!result.Success || result.Value is null) return null;
        if (result.Value.OrganiserId != User.GetUserId()) return null;

        var e = result.Value;
        return new EventDto(e.EventId, e.OrganiserId, e.OrganiserName, e.Name,
            e.Description, e.EventDate, e.Location, e.DistanceKm, e.EventType, e.BannerUrl);
    }
}
