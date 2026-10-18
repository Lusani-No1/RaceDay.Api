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
public class ResultsController : ControllerBase
{
    private readonly AppDbContext _db;

    public ResultsController(AppDbContext db) => _db = db;

    /// <summary>Captures a participant's finish time and finishing position after the event.</summary>
    /// <response code="201">Created. The new record is returned.</response>
    /// <response code="400">The request body failed validation.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Only the Organiser who owns the event can capture results.</response>
    /// <response code="404">Enrolment not found.</response>
    /// <response code="409">A result was already captured, or the enrolment was cancelled.</response>
    [HttpPost("/api/enrolments/{enrolmentId:int}/results")]
    [Authorize(Roles = "Organiser")]
    [ProducesResponseType(typeof(ResultResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Capture(int enrolmentId, ResultRequest request)
    {
        var enrolment = await _db.Enrolments
            .Include(e => e.Category).ThenInclude(c => c.Event)
            .FirstOrDefaultAsync(e => e.EnrolmentId == enrolmentId);

        if (enrolment is null) return NotFound();

        var organiserId = User.GetUserId();
        if (enrolment.Category.Event.OrganiserId != organiserId)
            return Problem(title: "You can only capture results for your own events.",
                statusCode: StatusCodes.Status403Forbidden);

        if (enrolment.Status == EnrolmentStatus.Cancelled)
            return Problem(title: "A result cannot be captured for a cancelled enrolment.",
                statusCode: StatusCodes.Status409Conflict);

        if (await _db.Results.AnyAsync(r => r.EnrolmentId == enrolmentId))
            return Problem(title: "A result has already been captured for this enrolment.",
                statusCode: StatusCodes.Status409Conflict);

        var result = new Result
        {
            EnrolmentId = enrolmentId,
            CapturedByUserId = organiserId,
            FinishTime = request.FinishTime!.Value,
            Position = request.Position!.Value
        };
        _db.Results.Add(result);

        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            return Problem(title: "A result has already been captured for this enrolment.",
                statusCode: StatusCodes.Status409Conflict);
        }

        var created = await WithDetails().FirstAsync(r => r.ResultId == result.ResultId);
        return Created($"/api/results/{created.ResultId}", ToResponse(created));
    }

    /// <summary>Corrects a previously captured result on an event the Organiser owns.</summary>
    /// <response code="200">Success.</response>
    /// <response code="400">Validation failed.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Only the Organiser who owns the event can change its results.</response>
    /// <response code="404">Result not found.</response>
    [HttpPut("/api/results/{id:int}")]
    [Authorize(Roles = "Organiser")]
    [ProducesResponseType(typeof(ResultResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(int id, ResultRequest request)
    {
        var result = await _db.Results
            .Include(r => r.Enrolment).ThenInclude(e => e.Category).ThenInclude(c => c.Event)
            .FirstOrDefaultAsync(r => r.ResultId == id);

        if (result is null) return NotFound();
        if (result.Enrolment.Category.Event.OrganiserId != User.GetUserId())
            return Problem(title: "You can only change results for your own events.",
                statusCode: StatusCodes.Status403Forbidden);

        result.FinishTime = request.FinishTime!.Value;
        result.Position = request.Position!.Value;
        await _db.SaveChangesAsync();

        var updated = await WithDetails().FirstAsync(r => r.ResultId == id);
        return Ok(ToResponse(updated));
    }

    /// <summary>Lists the logged-in Participant's own results across all events.</summary>
    /// <response code="200">Success.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Only Participants can do this.</response>
    [HttpGet("/api/results/me")]
    [Authorize(Roles = "Participant")]
    [ProducesResponseType(typeof(IEnumerable<ResultResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetMine()
    {
        var userId = User.GetUserId();
        var results = await WithDetails()
            .Where(r => r.Enrolment.ParticipantId == userId)
            .OrderByDescending(r => r.Enrolment.Category.Event.EventDate)
            .ToListAsync();

        return Ok(results.Select(ToResponse));
    }

    /// <summary>Lists all captured results for an event the Organiser owns.</summary>
    /// <response code="200">Success.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Only the Organiser who owns the event can view its results.</response>
    /// <response code="404">Event not found.</response>
    [HttpGet("/api/events/{eventId:int}/results")]
    [Authorize(Roles = "Organiser")]
    [ProducesResponseType(typeof(IEnumerable<ResultResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetForEvent(int eventId)
    {
        var ev = await _db.Events.AsNoTracking().FirstOrDefaultAsync(e => e.EventId == eventId);
        if (ev is null) return NotFound();
        if (ev.OrganiserId != User.GetUserId())
            return Problem(title: "You can only view results for your own events.",
                statusCode: StatusCodes.Status403Forbidden);

        var results = await WithDetails()
            .Where(r => r.Enrolment.Category.EventId == eventId)
            .OrderBy(r => r.Enrolment.CategoryId).ThenBy(r => r.Position)
            .ToListAsync();

        return Ok(results.Select(ToResponse));
    }

    private IQueryable<Result> WithDetails() => _db.Results.AsNoTracking()
        .Include(r => r.Enrolment).ThenInclude(e => e.Participant)
        .Include(r => r.Enrolment).ThenInclude(e => e.Category).ThenInclude(c => c.Event);

    private static ResultResponse ToResponse(Result r) => new(
        r.ResultId, r.EnrolmentId,
        r.Enrolment.ParticipantId, r.Enrolment.Participant.FullName,
        r.Enrolment.Category.EventId, r.Enrolment.Category.Event.Name,
        r.Enrolment.CategoryId, r.Enrolment.Category.Name,
        r.FinishTime, r.Position);
}
