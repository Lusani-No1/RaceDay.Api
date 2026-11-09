using Microsoft.AspNetCore.Mvc;
using RaceDay.Mvc.Helpers;
using RaceDay.Mvc.Services;
using RaceDay.Mvc.ViewModels;

namespace RaceDay.Mvc.Controllers;

public class ProfileController : Controller
{
    private readonly RaceDayApiClient _api;

    public ProfileController(RaceDayApiClient api) => _api = api;

    public async Task<IActionResult> Index()
    {
        var result = await _api.GetProfileAsync();
        if (!result.Success || result.Value is null)
        {
            TempData["Error"] = result.Error ?? "Could not load your profile.";
            return NotFound();
        }

        return View(new ProfileViewModel { Profile = result.Value });
    }

    [HttpGet]
    public async Task<IActionResult> Edit()
    {
        var result = await _api.GetProfileAsync();
        if (!result.Success || result.Value is null)
        {
            TempData["Error"] = result.Error ?? "Could not load your profile.";
            return RedirectToAction(nameof(Index));
        }

        var profile = result.Value;
        return View(new EditProfileViewModel
        {
            FullName = profile.FullName,
            PhoneNumber = profile.PhoneNumber,
            DateOfBirth = profile.DateOfBirth,
            EmergencyContact = profile.EmergencyContact,
            ProfileImageUrl = profile.ProfileImageUrl
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(EditProfileViewModel model)
    {
        if (model.DateOfBirth is { } dob && dob > DateOnly.FromDateTime(DateTime.Today))
            ModelState.AddModelError(nameof(model.DateOfBirth), "Date of birth cannot be in the future.");

        if (!ModelState.IsValid) return View(model);

        var result = await _api.UpdateProfileAsync(model.ToRequest());
        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Error ?? "Could not save your profile.");
            return View(model);
        }

        TempData["Success"] = "Your profile has been updated.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Picture(IFormFile? picture)
    {
        var error = ImageValidation.Validate(picture);
        if (error is not null)
        {
            TempData["Error"] = error;
            return RedirectToAction(nameof(Index));
        }

        var result = await _api.UploadProfilePictureAsync(picture!);
        TempData[result.Success ? "Success" : "Error"] = result.Success
            ? "Profile picture updated."
            : result.Error ?? "Could not upload that picture.";

        return RedirectToAction(nameof(Index));
    }
}
