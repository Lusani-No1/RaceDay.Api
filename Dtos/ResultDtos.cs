using System.ComponentModel.DataAnnotations;

namespace RaceDay.Api.Dtos;

public class ResultRequest : IValidatableObject
{
    /// <summary>Finish time as hh:mm:ss.</summary>
    /// <example>00:48:32</example>
    [Required]
    public TimeSpan? FinishTime { get; set; }

    /// <summary>Finishing position within the category (1 = first).</summary>
    /// <example>1</example>
    [Required, Range(1, 100000)]
    public int? Position { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (FinishTime is { } t && (t <= TimeSpan.Zero || t >= TimeSpan.FromHours(24)))
        {
            yield return new ValidationResult(
                "Finish time must be greater than 00:00:00 and less than 24 hours.",
                new[] { nameof(FinishTime) });
        }
    }
}

public record ResultResponse(
    int ResultId,
    int EnrolmentId,
    int ParticipantId,
    string ParticipantName,
    int EventId,
    string EventName,
    DateOnly EventDate,
    int CategoryId,
    string CategoryName,
    TimeSpan FinishTime,
    int Position,
    int TotalFinishers);
