using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RaceDay.Mvc.Extensions;
using RaceDay.Mvc.Helpers;
using RaceDay.Mvc.Services;
using RaceDay.Mvc.ViewModels;

namespace RaceDay.Mvc.Controllers;

[Authorize(Roles = "Organiser")]
[Route("organiser/events")]
public class OrganiserEventsController : Controller
{
    private readonly RaceDayApiClient _api;

    public OrganiserEventsController(RaceDayApiClient api) => _api = api;

    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
        var result = await _api.GetEventsAsync();
        if (!result.Success || result.Value is null)
        {
            TempData["Error"] = result.Error ?? "Could not load your events.";
            return View(new List<Models.EventDto>());
        }

        return View(result.Value
            .Where(e => e.OrganiserId == User.GetUserId())
            .OrderBy(e => e.EventDate)
            .ToList());
    }

    [HttpGet("create")]
    public IActionResult Create() => View(new EventFormViewModel
    {
        EventDate = DateOnly.FromDateTime(DateTime.Today.AddDays(30))
    });

    [HttpPost("create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(EventFormViewModel model)
    {
        ValidateBanner(model);
        if (!ModelState.IsValid) return View(model);

        var created = await _api.CreateEventAsync(model.ToRequest());
        if (!created.Success || created.Value is null)
        {
            ModelState.AddModelError(string.Empty, created.Error ?? "Could not create the event.");
            return View(model);
        }

        if (model.Banner is not null)
        {
            var upload = await _api.UploadBannerAsync(created.Value.EventId, model.Banner);
            if (!upload.Success)
                TempData["Error"] = upload.Error ?? "Event created, but the banner upload failed.";
        }

        TempData["Success"] = "Event created.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet("edit/{id:int}")]
    public async Task<IActionResult> Edit(int id)
    {
        var result = await _api.GetEventAsync(id);
        if (!result.Success || result.Value is null) return NotFound();
        if (result.Value.OrganiserId != User.GetUserId()) return Forbid();

        var ev = result.Value;
        return View(new EventFormViewModel
        {
            EventId = ev.EventId,
            Name = ev.Name,
            Description = ev.Description,
            EventDate = ev.EventDate,
            Location = ev.Location,
            DistanceKm = ev.DistanceKm,
            EventType = ev.EventType,
            ExistingBannerUrl = ev.BannerUrl
        });
    }

    [HttpPost("edit/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, EventFormViewModel model)
    {
        ValidateBanner(model);
        if (!ModelState.IsValid) return View(model);

        var current = await _api.GetEventAsync(id);
        if (!current.Success || current.Value is null) return NotFound();
        if (current.Value.OrganiserId != User.GetUserId()) return Forbid();

        var updated = await _api.UpdateEventAsync(id, model.ToRequest());
        if (!updated.Success)
        {
            ModelState.AddModelError(string.Empty, updated.Error ?? "Could not update the event.");
            return View(model);
        }

        if (model.Banner is not null)
        {
            var upload = await _api.UploadBannerAsync(id, model.Banner);
            if (!upload.Success)
                TempData["Error"] = upload.Error ?? "Event saved, but the banner upload failed.";
        }

        TempData["Success"] = "Event updated.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("delete/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _api.DeleteEventAsync(id);
        TempData[result.Success ? "Success" : "Error"] = result.Success
            ? "Event deleted."
            : result.Error ?? "Could not delete the event.";

        return RedirectToAction(nameof(Index));
    }

    private void ValidateBanner(EventFormViewModel model)
    {
        if (model.Banner is null) return;

        var error = ImageValidation.Validate(model.Banner);
        if (error is not null)
            ModelState.AddModelError(nameof(model.Banner), error);
    }
}
