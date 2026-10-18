using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos;
using RaceDay.Api.Extensions;
using RaceDay.Api.Models;
using AppUser = RaceDay.Api.Models.User;

namespace RaceDay.Api.Controllers;

[ApiController]
[Route("api/users")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly AppDbContext _db;

    public UsersController(AppDbContext db) => _db = db;

    /// <summary>Returns the logged-in user's details and profile.</summary>
    /// <response code="200">Success.</response>
    /// <response code="401">Not logged in.</response>
    [HttpGet("me")]
    [ProducesResponseType(typeof(ProfileResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetMe()
    {
        var id = User.GetUserId();
        var user = await _db.Users.Include(u => u.Profile).AsNoTracking()
            .FirstOrDefaultAsync(u => u.UserId == id);

        if (user is null) return Unauthorized();
        return Ok(ToResponse(user));
    }

    /// <summary>Updates the logged-in user's own profile information.</summary>
    /// <response code="200">Success.</response>
    /// <response code="400">Validation failed, e.g. date of birth in the future.</response>
    /// <response code="401">Not logged in.</response>
    [HttpPut("me")]
    [ProducesResponseType(typeof(ProfileResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> UpdateMe(UpdateProfileRequest request)
    {
        if (request.DateOfBirth is { } dob && dob > DateOnly.FromDateTime(DateTime.UtcNow))
        {
            ModelState.AddModelError(nameof(request.DateOfBirth), "Date of birth cannot be in the future.");
            return ValidationProblem(ModelState);
        }

        var id = User.GetUserId();
        var user = await _db.Users.Include(u => u.Profile).FirstOrDefaultAsync(u => u.UserId == id);
        if (user is null) return Unauthorized();

        user.Profile ??= new UserProfile();
        user.FullName = request.FullName.Trim();
        user.Profile.PhoneNumber = request.PhoneNumber;
        user.Profile.DateOfBirth = request.DateOfBirth;
        user.Profile.EmergencyContact = request.EmergencyContact;
        user.Profile.ProfileImageUrl = request.ProfileImageUrl;

        await _db.SaveChangesAsync();
        return Ok(ToResponse(user));
    }

    private static ProfileResponse ToResponse(AppUser u) => new(
        u.UserId, u.FullName, u.Email, u.Role,
        u.Profile?.PhoneNumber, u.Profile?.DateOfBirth,
        u.Profile?.EmergencyContact, u.Profile?.ProfileImageUrl,
        u.CreatedAt);
}
