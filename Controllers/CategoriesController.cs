using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos;
using RaceDay.Api.Extensions;
using RaceDay.Api.Models;

namespace RaceDay.Api.Controllers;

[ApiController]
[Authorize]
public class CategoriesController : ControllerBase
{
    private readonly AppDbContext _db;

    public CategoriesController(AppDbContext db) => _db = db;

    /// <summary>Lists the categories available for an event.</summary>
    /// <response code="200">Success.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="404">Event not found.</response>
    [HttpGet("/api/events/{eventId:int}/categories")]
    [ProducesResponseType(typeof(IEnumerable<CategoryResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetForEvent(int eventId)
    {
        if (!await _db.Events.AnyAsync(e => e.EventId == eventId)) return NotFound();

        var categories = await _db.Categories.AsNoTracking()
            .Where(c => c.EventId == eventId)
            .OrderBy(c => c.CategoryId)
            .ToListAsync();

        return Ok(categories.Select(ToResponse));
    }

    /// <summary>Adds a distance and/or age category to an event the Organiser owns.</summary>
    /// <response code="201">Created. The new record is returned.</response>
    /// <response code="400">Validation failed. A category needs a distance and/or an age limit, and MinAge cannot exceed MaxAge.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Only the Organiser who owns the event can add categories.</response>
    /// <response code="404">Event not found.</response>
    [HttpPost("/api/events/{eventId:int}/categories")]
    [Authorize(Roles = "Organiser")]
    [ProducesResponseType(typeof(CategoryResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Create(int eventId, CategoryRequest request)
    {
        var ev = await _db.Events.FirstOrDefaultAsync(e => e.EventId == eventId);
        if (ev is null) return NotFound();
        if (ev.OrganiserId != User.GetUserId())
            return Problem(title: "You can only add categories to your own events.",
                statusCode: StatusCodes.Status403Forbidden);

        var category = new Category
        {
            EventId = eventId,
            Name = request.Name.Trim(),
            DistanceKm = request.DistanceKm,
            MinAge = request.MinAge,
            MaxAge = request.MaxAge,
            Price = request.Price
        };

        _db.Categories.Add(category);
        await _db.SaveChangesAsync();

        return Created($"/api/categories/{category.CategoryId}", ToResponse(category));
    }

    /// <summary>Updates a category on an event the Organiser owns.</summary>
    /// <response code="200">Success.</response>
    /// <response code="400">Validation failed. A category needs a distance and/or an age limit, and MinAge cannot exceed MaxAge.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Only the Organiser who owns the event can change its categories.</response>
    /// <response code="404">Category not found.</response>
    [HttpPut("/api/categories/{id:int}")]
    [Authorize(Roles = "Organiser")]
    [ProducesResponseType(typeof(CategoryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(int id, CategoryRequest request)
    {
        var category = await _db.Categories.Include(c => c.Event).FirstOrDefaultAsync(c => c.CategoryId == id);
        if (category is null) return NotFound();
        if (category.Event.OrganiserId != User.GetUserId())
            return Problem(title: "You can only change categories on your own events.",
                statusCode: StatusCodes.Status403Forbidden);

        category.Name = request.Name.Trim();
        category.DistanceKm = request.DistanceKm;
        category.MinAge = request.MinAge;
        category.MaxAge = request.MaxAge;
        category.Price = request.Price;

        await _db.SaveChangesAsync();
        return Ok(ToResponse(category));
    }

    /// <summary>Removes a category from an event the Organiser owns.</summary>
    /// <response code="204">Success. Nothing is returned.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Only the Organiser who owns the event can delete its categories.</response>
    /// <response code="404">Category not found.</response>
    /// <response code="409">The category already has enrolments, so it cannot be deleted.</response>
    [HttpDelete("/api/categories/{id:int}")]
    [Authorize(Roles = "Organiser")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(int id)
    {
        var category = await _db.Categories.Include(c => c.Event).FirstOrDefaultAsync(c => c.CategoryId == id);
        if (category is null) return NotFound();
        if (category.Event.OrganiserId != User.GetUserId())
            return Problem(title: "You can only delete categories on your own events.",
                statusCode: StatusCodes.Status403Forbidden);

        if (await _db.Enrolments.AnyAsync(en => en.CategoryId == id))
            return Problem(title: "This category already has enrolments and cannot be deleted.",
                statusCode: StatusCodes.Status409Conflict);

        _db.Categories.Remove(category);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    // Shared with EventsController so both return categories in the same shape
    public static CategoryResponse ToResponse(Category c) => new(
        c.CategoryId, c.EventId, c.Name, c.DistanceKm, c.MinAge, c.MaxAge, c.Price);
}
