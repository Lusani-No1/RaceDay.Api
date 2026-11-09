using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RaceDay.Mvc.Extensions;
using RaceDay.Mvc.Services;
using RaceDay.Mvc.ViewModels;

namespace RaceDay.Mvc.Controllers;

[Authorize(Roles = "Organiser")]
public class OrganiserController : Controller
{
    private readonly RaceDayApiClient _api;

    public OrganiserController(RaceDayApiClient api) => _api = api;

    public async Task<IActionResult> Index()
    {
        var events = await _api.GetEventsAsync();
        if (!events.Success || events.Value is null)
        {
            TempData["Error"] = events.Error ?? "Could not load your dashboard.";
            return View(new OrganiserDashboardViewModel());
        }

        var mine = events.Value
            .Where(e => e.OrganiserId == User.GetUserId())
            .OrderBy(e => e.EventDate)
            .ToList();

        var summaries = new List<OrganiserEventSummary>();
        foreach (var ev in mine)
        {
            var enrolments = await _api.GetEventEnrolmentsAsync(ev.EventId);
            summaries.Add(new OrganiserEventSummary(ev, enrolments.Value?.Count ?? 0));
        }

        return View(new OrganiserDashboardViewModel { Events = summaries });
    }
}
