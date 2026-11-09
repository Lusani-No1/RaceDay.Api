using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RaceDay.Mvc.Services;

namespace RaceDay.Mvc.Controllers;

[Authorize(Roles = "Participant")]
public class ResultsController : Controller
{
    private readonly RaceDayApiClient _api;

    public ResultsController(RaceDayApiClient api) => _api = api;

    public async Task<IActionResult> Mine()
    {
        var result = await _api.GetMyResultsAsync();
        if (!result.Success || result.Value is null)
            TempData["Error"] = result.Error ?? "Could not load your results.";

        return View(result.Value ?? new());
    }
}
