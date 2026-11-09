using Microsoft.AspNetCore.Mvc;
using RaceDay.Mvc.Services;
using RaceDay.Mvc.ViewModels;

namespace RaceDay.Mvc.Controllers;

public class EventsController : Controller
{
    private readonly RaceDayApiClient _api;

    public EventsController(RaceDayApiClient api) => _api = api;

    public async Task<IActionResult> Index(string? search, string? type, DateOnly? from)
    {
        var result = await _api.GetEventsAsync();
        if (!result.Success || result.Value is null)
        {
            TempData["Error"] = result.Error ?? "Could not load events.";
            return View(new EventListViewModel());
        }

        var events = result.Value.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(search))
            events = events.Where(e =>
                e.Name.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                e.Location.Contains(search, StringComparison.OrdinalIgnoreCase));

        if (!string.IsNullOrWhiteSpace(type) && type is not "All")
            events = events.Where(e => e.EventType == type);

        if (from is not null)
            events = events.Where(e => e.EventDate >= from);

        return View(new EventListViewModel
        {
            Events = events.OrderBy(e => e.EventDate).ToList(),
            Search = search,
            Type = type,
            From = from
        });
    }

    public async Task<IActionResult> Details(int id)
    {
        var result = await _api.GetEventAsync(id);
        if (!result.Success || result.Value is null) return NotFound();

        var model = new EventDetailsViewModel { Event = result.Value };

        if (User.IsInRole("Participant"))
        {
            var mine = await _api.GetMyEnrolmentsAsync();
            var enrolment = mine.Value?.FirstOrDefault(e =>
                e.EventId == id && e.Status != "Cancelled");

            model.AlreadyEnrolled = enrolment is not null;
            model.EnrolmentId = enrolment?.EnrolmentId;
        }

        return View(model);
    }
}
