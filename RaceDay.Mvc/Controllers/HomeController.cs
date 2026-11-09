using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RaceDay.Mvc.Models;
using RaceDay.Mvc.Services;
using RaceDay.Mvc.ViewModels;

namespace RaceDay.Mvc.Controllers;

public class HomeController : Controller
{
    private readonly RaceDayApiClient _api;

    public HomeController(RaceDayApiClient api) => _api = api;

    public async Task<IActionResult> Index(string? search, string? type)
    {
        // Organisers land on their dashboard, Participants on the event home page.
        if (User.IsInRole("Organiser"))
            return RedirectToAction("Index", "Organiser");

        var result = await _api.GetEventsAsync();
        if (!result.Success || result.Value is null)
        {
            TempData["Error"] = result.Error ?? "Could not load events.";
            return View(new EventListViewModel { UpcomingOnly = true });
        }

        var today = DateOnly.FromDateTime(DateTime.Today);
        var events = result.Value
            .Where(e => e.EventDate >= today)
            .AsEnumerable();

        if (!string.IsNullOrWhiteSpace(search))
            events = events.Where(e =>
                e.Name.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                e.Location.Contains(search, StringComparison.OrdinalIgnoreCase));

        if (!string.IsNullOrWhiteSpace(type) && type is not "All")
            events = events.Where(e => e.EventType == type);

        return View(new EventListViewModel
        {
            Events = events.OrderBy(e => e.EventDate).ToList(),
            Search = search,
            Type = type,
            UpcomingOnly = true
        });
    }

    [AllowAnonymous]
    public IActionResult Error() => View();
}
