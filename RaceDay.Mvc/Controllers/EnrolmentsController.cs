using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RaceDay.Mvc.Services;

namespace RaceDay.Mvc.Controllers;

[Authorize(Roles = "Participant")]
public class EnrolmentsController : Controller
{
    private readonly RaceDayApiClient _api;

    public EnrolmentsController(RaceDayApiClient api) => _api = api;

    public async Task<IActionResult> Index()
    {
        var result = await _api.GetMyEnrolmentsAsync();
        if (!result.Success || result.Value is null)
            TempData["Error"] = result.Error ?? "Could not load your enrolments.";

        return View(result.Value ?? new());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Enrol(int categoryId, int eventId)
    {
        var result = await _api.EnrolAsync(categoryId);

        TempData[result.Success ? "Success" : "Error"] = result.Success
            ? "You are enrolled. Good luck!"
            : result.Error ?? "Could not complete your enrolment.";

        return RedirectToAction("Details", "Events", new { id = eventId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(int id)
    {
        var result = await _api.CancelEnrolmentAsync(id);

        TempData[result.Success ? "Success" : "Error"] = result.Success
            ? "Your enrolment has been cancelled."
            : result.Error ?? "Could not cancel that enrolment.";

        return RedirectToAction(nameof(Index));
    }
}
