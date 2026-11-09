using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RaceDay.Mvc.Extensions;
using RaceDay.Mvc.Models;
using RaceDay.Mvc.Services;
using RaceDay.Mvc.ViewModels;

namespace RaceDay.Mvc.Controllers;

[Authorize(Roles = "Organiser")]
[Route("organiser/events/{eventId:int}/enrolments")]
public class OrganiserEnrolmentsController : Controller
{
    private readonly RaceDayApiClient _api;

    public OrganiserEnrolmentsController(RaceDayApiClient api) => _api = api;

    [HttpGet("")]
    public async Task<IActionResult> Index(int eventId)
    {
        var eventResult = await _api.GetEventAsync(eventId);
        if (!eventResult.Success || eventResult.Value is null) return NotFound();
        if (eventResult.Value.OrganiserId != User.GetUserId()) return Forbid();

        var enrolments = await _api.GetEventEnrolmentsAsync(eventId);
        if (!enrolments.Success)
            TempData["Error"] = enrolments.Error ?? "Could not load enrolments.";

        return View(new EventEnrolmentsViewModel
        {
            Event = ToDto(eventResult.Value),
            Enrolments = enrolments.Value ?? new List<EnrolmentDto>()
        });
    }

    private static EventDto ToDto(EventDetailDto e) => new(
        e.EventId, e.OrganiserId, e.OrganiserName, e.Name, e.Description,
        e.EventDate, e.Location, e.DistanceKm, e.EventType, e.BannerUrl);
}
