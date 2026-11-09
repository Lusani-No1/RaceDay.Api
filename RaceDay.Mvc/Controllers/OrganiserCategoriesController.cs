using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RaceDay.Mvc.Extensions;
using RaceDay.Mvc.Models;
using RaceDay.Mvc.Services;
using RaceDay.Mvc.ViewModels;

namespace RaceDay.Mvc.Controllers;

[Authorize(Roles = "Organiser")]
[Route("organiser/events/{eventId:int}/categories")]
public class OrganiserCategoriesController : Controller
{
    private readonly RaceDayApiClient _api;

    public OrganiserCategoriesController(RaceDayApiClient api) => _api = api;

    [HttpGet("")]
    public async Task<IActionResult> Index(int eventId)
    {
        var ev = await GetOwnedEventAsync(eventId);
        if (ev is null) return NotFound();

        var categories = await _api.GetCategoriesAsync(eventId);
        if (!categories.Success)
            TempData["Error"] = categories.Error ?? "Could not load categories.";

        ViewData["Event"] = ev;
        return View(categories.Value ?? new List<CategoryDto>());
    }

    [HttpGet("create")]
    public async Task<IActionResult> Create(int eventId)
    {
        var ev = await GetOwnedEventAsync(eventId);
        if (ev is null) return NotFound();

        return View(new CategoryFormViewModel { EventId = eventId, EventName = ev.Name });
    }

    [HttpPost("create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(int eventId, CategoryFormViewModel model)
    {
        model.EventId = eventId;
        var ev = await GetOwnedEventAsync(eventId);
        if (ev is null) return NotFound();
        model.EventName = ev.Name;

        if (!ModelState.IsValid) return View(model);

        var result = await _api.CreateCategoryAsync(eventId, model.ToRequest());
        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Error ?? "Could not add the category.");
            return View(model);
        }

        TempData["Success"] = "Category added.";
        return RedirectToAction(nameof(Index), new { eventId });
    }

    [HttpGet("edit/{id:int}")]
    public async Task<IActionResult> Edit(int eventId, int id)
    {
        var ev = await GetOwnedEventAsync(eventId);
        if (ev is null) return NotFound();

        var categories = await _api.GetCategoriesAsync(eventId);
        var category = categories.Value?.FirstOrDefault(c => c.CategoryId == id);
        if (category is null) return NotFound();

        return View(new CategoryFormViewModel
        {
            CategoryId = category.CategoryId,
            EventId = eventId,
            EventName = ev.Name,
            Name = category.Name,
            DistanceKm = category.DistanceKm,
            MinAge = category.MinAge,
            MaxAge = category.MaxAge,
            Price = category.Price
        });
    }

    [HttpPost("edit/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int eventId, int id, CategoryFormViewModel model)
    {
        model.EventId = eventId;
        model.CategoryId = id;
        var ev = await GetOwnedEventAsync(eventId);
        if (ev is null) return NotFound();
        model.EventName = ev.Name;

        if (!ModelState.IsValid) return View(model);

        var result = await _api.UpdateCategoryAsync(id, model.ToRequest());
        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Error ?? "Could not update the category.");
            return View(model);
        }

        TempData["Success"] = "Category updated.";
        return RedirectToAction(nameof(Index), new { eventId });
    }

    [HttpPost("delete/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int eventId, int id)
    {
        var result = await _api.DeleteCategoryAsync(id);
        TempData[result.Success ? "Success" : "Error"] = result.Success
            ? "Category deleted."
            : result.Error ?? "Could not delete the category.";

        return RedirectToAction(nameof(Index), new { eventId });
    }

    private async Task<EventDto?> GetOwnedEventAsync(int eventId)
    {
        var result = await _api.GetEventAsync(eventId);
        if (!result.Success || result.Value is null) return null;
        if (result.Value.OrganiserId != User.GetUserId()) return null;

        return new EventDto(
            result.Value.EventId, result.Value.OrganiserId, result.Value.OrganiserName,
            result.Value.Name, result.Value.Description, result.Value.EventDate,
            result.Value.Location, result.Value.DistanceKm, result.Value.EventType,
            result.Value.BannerUrl);
    }
}
