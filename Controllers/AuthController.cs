using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos;
using RaceDay.Api.Models;
using AppUser = RaceDay.Api.Models.User;

namespace RaceDay.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IPasswordHasher<AppUser> _hasher;
    private readonly IConfiguration _config;

    public AuthController(AppDbContext db, IPasswordHasher<AppUser> hasher, IConfiguration config)
    {
        _db = db;
        _hasher = hasher;
        _config = config;
    }

    /// <summary>Registers a new account as an Organiser or Participant.</summary>
    /// <remarks>Registering as an Organiser also requires the organiserAccessCode.</remarks>
    /// <response code="201">Created. The new record is returned.</response>
    /// <response code="400">Validation failed, e.g. invalid email or a password shorter than 8 characters.</response>
    /// <response code="403">Registering as an Organiser needs a valid organiserAccessCode.</response>
    /// <response code="409">That email is already registered.</response>
    [HttpPost("register")]
    [ProducesResponseType(typeof(UserResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Register(RegisterRequest request)
    {
        var role = request.Role!.Value;

        if (role == UserRole.Organiser)
        {
            var expected = _config["Auth:OrganiserAccessCode"];
            if (string.IsNullOrEmpty(expected) || request.OrganiserAccessCode != expected)
                return Problem(title: "Invalid organiser access code.", statusCode: StatusCodes.Status403Forbidden);
        }

        var email = request.Email.Trim().ToLowerInvariant();
        if (await _db.Users.AnyAsync(u => u.Email == email))
            return Problem(title: "That email is already registered.", statusCode: StatusCodes.Status409Conflict);

        var user = new AppUser
        {
            FullName = request.FullName.Trim(),
            Email = email,
            Role = role,
            Profile = new UserProfile()
        };
        user.PasswordHash = _hasher.HashPassword(user, request.Password);

        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        return StatusCode(StatusCodes.Status201Created,
            new UserResponse(user.UserId, user.FullName, user.Email, user.Role));
    }

    /// <summary>Checks credentials and starts a session (cookie) holding the user's id and role.</summary>
    /// <response code="200">Success.</response>
    /// <response code="400">The request body failed validation.</response>
    /// <response code="401">Invalid email or password.</response>
    [HttpPost("login")]
    [ProducesResponseType(typeof(UserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login(LoginRequest request)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == email);

        // Same message for "no such user" and "wrong password" so attackers can't tell which emails exist.
        if (user is null ||
            _hasher.VerifyHashedPassword(user, user.PasswordHash, request.Password) == PasswordVerificationResult.Failed)
        {
            return Problem(title: "Invalid email or password.", statusCode: StatusCodes.Status401Unauthorized);
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.UserId.ToString()),
            new(ClaimTypes.Name, user.FullName),
            new(ClaimTypes.Role, user.Role.ToString())
        };
        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity));

        return Ok(new UserResponse(user.UserId, user.FullName, user.Email, user.Role));
    }

    /// <summary>Ends the current session.</summary>
    /// <response code="204">Success. Nothing is returned.</response>
    /// <response code="401">There is no active session.</response>
    [Authorize]
    [HttpPost("logout")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return NoContent();
    }
}
