using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos;
using RaceDay.Api.Extensions;
using RaceDay.Api.Models;
using RaceDay.Api.Services;

namespace RaceDay.Api.Controllers;

[ApiController]
[Route("api/events")]
[Authorize]
public class EventsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IFileStorage _storage;

    public EventsController(AppDbContext db, IFileStorage storage)
    {
        _db = db;
        _storage = storage;
    }

    /// <summary>Lists all events, soonest first.</summary>
    /// <response code="200">Success.</response>
    /// <response code="401">Not logged in.</response>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<EventResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetAll()
    {
        var events = await _db.Events.AsNoTracking()
            .Include(e => e.Organiser)
            .OrderBy(e => e.EventDate)
            .ToListAsync();

        return Ok(events.Select(ToResponse));
    }

    /// <summary>Returns one event together with its categories.</summary>
    /// <response code="200">Success.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="404">Event not found.</response>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(EventDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id)
    {
        var ev = await _db.Events.AsNoTracking()
            .Include(e => e.Organiser)
            .Include(e => e.Categories)
            .FirstOrDefaultAsync(e => e.EventId == id);

        if (ev is null) return NotFound();

        return Ok(new EventDetailResponse(
            ev.EventId, ev.OrganiserId, ev.Organiser.FullName, ev.Name, ev.Description,
            ev.EventDate, ev.Location, ev.DistanceKm, ev.EventType, ev.BannerUrl,
            ev.Categories.OrderBy(c => c.CategoryId).Select(CategoriesController.ToResponse).ToList()));
    }

    /// <summary>Creates a new event owned by the logged-in Organiser.</summary>
    /// <response code="201">Created. The new record is returned.</response>
    /// <response code="400">Validation failed.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Only Organisers can create events.</response>
    [HttpPost]
    [Authorize(Roles = "Organiser")]
    [ProducesResponseType(typeof(EventResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Create(EventRequest request)
    {
        var organiserId = User.GetUserId();

        var ev = new Event
        {
            OrganiserId = organiserId,
            Name = request.Name.Trim(),
            Description = request.Description,
            EventDate = request.EventDate!.Value,
            Location = request.Location.Trim(),
            DistanceKm = request.DistanceKm!.Value,
            EventType = request.EventType!.Value
        };

        _db.Events.Add(ev);
        await _db.SaveChangesAsync();
        await _db.Entry(ev).Reference(e => e.Organiser).LoadAsync();

        return CreatedAtAction(nameof(GetById), new { id = ev.EventId }, ToResponse(ev));
    }

    /// <summary>Updates an event. Only the Organiser who created it can do this.</summary>
    /// <response code="200">Success.</response>
    /// <response code="400">Validation failed.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Only the Organiser who created the event can change it.</response>
    /// <response code="404">Event not found.</response>
    [HttpPut("{id:int}")]
    [Authorize(Roles = "Organiser")]
    [ProducesResponseType(typeof(EventResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(int id, EventRequest request)
    {
        var ev = await _db.Events.Include(e => e.Organiser).FirstOrDefaultAsync(e => e.EventId == id);
        if (ev is null) return NotFound();
        if (ev.OrganiserId != User.GetUserId())
            return Problem(title: "You can only change your own events.", statusCode: StatusCodes.Status403Forbidden);

        ev.Name = request.Name.Trim();
        ev.Description = request.Description;
        ev.EventDate = request.EventDate!.Value;
        ev.Location = request.Location.Trim();
        ev.DistanceKm = request.DistanceKm!.Value;
        ev.EventType = request.EventType!.Value;

        await _db.SaveChangesAsync();
        return Ok(ToResponse(ev));
    }

    /// <summary>Deletes an event and its categories. Only the owning Organiser can do this.</summary>
    /// <response code="204">Success. Nothing is returned.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Only the Organiser who created the event can delete it.</response>
    /// <response code="404">Event not found.</response>
    /// <response code="409">The event already has enrolments, so it cannot be deleted.</response>
    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Organiser")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(int id)
    {
        var ev = await _db.Events.FirstOrDefaultAsync(e => e.EventId == id);
        if (ev is null) return NotFound();
        if (ev.OrganiserId != User.GetUserId())
            return Problem(title: "You can only delete your own events.", statusCode: StatusCodes.Status403Forbidden);

        // Enrolments are not cascade-deleted (see ERD/SQL), so protect participants' entries
        if (await _db.Enrolments.AnyAsync(en => en.Category.EventId == id))
            return Problem(title: "This event already has enrolments and cannot be deleted.",
                statusCode: StatusCodes.Status409Conflict);

        _db.Events.Remove(ev);   // its categories are removed by the database cascade
        await _db.SaveChangesAsync();
        return NoContent();
    }

    /// <summary>Uploads or replaces the banner image for an event the Organiser owns.</summary>
    /// <response code="200">Success. The updated event is returned.</response>
    /// <response code="400">The image is missing, too large, or not a supported type.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Only the Organiser who owns the event can upload its banner.</response>
    /// <response code="404">Event not found.</response>
    [HttpPost("{id:int}/banner")]
    [Authorize(Roles = "Organiser")]
    [RequestSizeLimit(6_000_000)]
    [ProducesResponseType(typeof(EventResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UploadBanner(int id, IFormFile file, CancellationToken ct)
    {
        var validationError = ImageUpload.Validate(file);
        if (validationError is not null)
            return Problem(title: validationError, statusCode: StatusCodes.Status400BadRequest);

        var ev = await _db.Events.FirstOrDefaultAsync(e => e.EventId == id);
        if (ev is null) return NotFound();
        if (ev.OrganiserId != User.GetUserId())
            return Problem(title: "You can only change your own events.", statusCode: StatusCodes.Status403Forbidden);

        await using var stream = file.OpenReadStream();
        var url = await _storage.SaveAsync(stream, file.FileName, file.ContentType!, "event-banners", ct);

        if (!string.IsNullOrWhiteSpace(ev.BannerUrl))
            await _storage.DeleteAsync(ev.BannerUrl, ct);

        ev.BannerUrl = url;
        await _db.SaveChangesAsync();
        await _db.Entry(ev).Reference(e => e.Organiser).LoadAsync();

        return Ok(ToResponse(ev));
    }

    private static EventResponse ToResponse(Event e) => new(
        e.EventId, e.OrganiserId, e.Organiser.FullName, e.Name, e.Description,
        e.EventDate, e.Location, e.DistanceKm, e.EventType, e.BannerUrl);
}
