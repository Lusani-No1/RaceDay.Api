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
public class EnrolmentsController : ControllerBase
{
    private readonly AppDbContext _db;

    public EnrolmentsController(AppDbContext db) => _db = db;

    /// <summary>Enrols the logged-in Participant in a category, linking participant, event and category.</summary>
    /// <response code="201">Created. The new record is returned.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Only Participants can enrol.</response>
    /// <response code="404">Category not found.</response>
    /// <response code="409">You are already enrolled in this category.</response>
    [HttpPost("/api/categories/{categoryId:int}/enrolments")]
    [Authorize(Roles = "Participant")]
    [ProducesResponseType(typeof(EnrolmentResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Enrol(int categoryId)
    {
        if (!await _db.Categories.AnyAsync(c => c.CategoryId == categoryId)) return NotFound();

        var userId = User.GetUserId();
        var enrolment = await _db.Enrolments
            .FirstOrDefaultAsync(e => e.ParticipantId == userId && e.CategoryId == categoryId);

        if (enrolment is not null && enrolment.Status != EnrolmentStatus.Cancelled)
            return Problem(title: "You are already enrolled in this category.",
                statusCode: StatusCodes.Status409Conflict);

        if (enrolment is null)
        {
            enrolment = new Enrolment
            {
                ParticipantId = userId,
                CategoryId = categoryId,
                Status = EnrolmentStatus.Confirmed
            };
            _db.Enrolments.Add(enrolment);
        }
        else
        {
            // Previously cancelled: re-activate the same record (a participant can only have one per category)
            enrolment.Status = EnrolmentStatus.Confirmed;
            enrolment.EnrolmentDate = DateTime.Now;
        }

        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            // Two requests at once hit the unique (participant, category) rule
            return Problem(title: "You are already enrolled in this category.",
                statusCode: StatusCodes.Status409Conflict);
        }

        var created = await WithDetails().FirstAsync(e => e.EnrolmentId == enrolment.EnrolmentId);
        return Created($"/api/enrolments/{created.EnrolmentId}", ToResponse(created));
    }

    /// <summary>Lists the logged-in Participant's own enrolments.</summary>
    /// <response code="200">Success.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Only Participants can do this.</response>
    [HttpGet("/api/enrolments/me")]
    [Authorize(Roles = "Participant")]
    [ProducesResponseType(typeof(IEnumerable<EnrolmentResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetMine()
    {
        var userId = User.GetUserId();
        var enrolments = await WithDetails()
            .Where(e => e.ParticipantId == userId)
            .OrderBy(e => e.Category.Event.EventDate)
            .ToListAsync();

        return Ok(enrolments.Select(ToResponse));
    }

    /// <summary>Lists all enrolments for an event the Organiser owns.</summary>
    /// <response code="200">Success.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Only the Organiser who owns the event can view its enrolments.</response>
    /// <response code="404">Event not found.</response>
    [HttpGet("/api/events/{eventId:int}/enrolments")]
    [Authorize(Roles = "Organiser")]
    [ProducesResponseType(typeof(IEnumerable<EnrolmentResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetForEvent(int eventId)
    {
        var ev = await _db.Events.AsNoTracking().FirstOrDefaultAsync(e => e.EventId == eventId);
        if (ev is null) return NotFound();
        if (ev.OrganiserId != User.GetUserId())
            return Problem(title: "You can only view enrolments for your own events.",
                statusCode: StatusCodes.Status403Forbidden);

        var enrolments = await WithDetails()
            .Where(e => e.Category.EventId == eventId)
            .OrderBy(e => e.CategoryId).ThenBy(e => e.EnrolmentId)
            .ToListAsync();

        return Ok(enrolments.Select(ToResponse));
    }

    /// <summary>Cancels the logged-in Participant's own enrolment.</summary>
    /// <response code="204">Success. Nothing is returned.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Only Participants can cancel, and only their own enrolments.</response>
    /// <response code="404">Enrolment not found.</response>
    /// <response code="409">A result has already been captured for this enrolment.</response>
    [HttpDelete("/api/enrolments/{id:int}")]
    [Authorize(Roles = "Participant")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Cancel(int id)
    {
        var enrolment = await _db.Enrolments.FirstOrDefaultAsync(e => e.EnrolmentId == id);
        if (enrolment is null) return NotFound();
        if (enrolment.ParticipantId != User.GetUserId())
            return Problem(title: "You can only cancel your own enrolments.",
                statusCode: StatusCodes.Status403Forbidden);

        if (await _db.Results.AnyAsync(r => r.EnrolmentId == id))
            return Problem(title: "A result has already been captured, so this enrolment cannot be cancelled.",
                statusCode: StatusCodes.Status409Conflict);

        // The record is kept (Status = Cancelled) so the history stays intact
        enrolment.Status = EnrolmentStatus.Cancelled;
        await _db.SaveChangesAsync();
        return NoContent();
    }

    private IQueryable<Enrolment> WithDetails() => _db.Enrolments.AsNoTracking()
        .Include(e => e.Participant)
        .Include(e => e.Category).ThenInclude(c => c.Event);

    private static EnrolmentResponse ToResponse(Enrolment e) => new(
        e.EnrolmentId, e.ParticipantId, e.Participant.FullName,
        e.CategoryId, e.Category.Name,
        e.Category.EventId, e.Category.Event.Name, e.Category.Event.EventDate,
        e.EnrolmentDate, e.Status);
}
