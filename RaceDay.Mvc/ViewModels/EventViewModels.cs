using System.ComponentModel.DataAnnotations;
using RaceDay.Mvc.Models;

namespace RaceDay.Mvc.ViewModels;

public class EventListViewModel
{
    public IReadOnlyList<EventDto> Events { get; set; } = Array.Empty<EventDto>();
    public string? Search { get; set; }
    public string? Type { get; set; }
    public DateOnly? From { get; set; }
    public bool UpcomingOnly { get; set; }
}

public class EventDetailsViewModel
{
    public EventDetailDto Event { get; set; } = null!;
    public bool AlreadyEnrolled { get; set; }
    public int? EnrolmentId { get; set; }
}

public class EventFormViewModel : IValidatableObject
{
    public int? EventId { get; set; }

    [Required, StringLength(150)]
    public string Name { get; set; } = string.Empty;

    [StringLength(1000)]
    public string? Description { get; set; }

    [Required, DataType(DataType.Date)]
    [Display(Name = "Event date")]
    public DateOnly? EventDate { get; set; }

    [Required, StringLength(150)]
    public string Location { get; set; } = string.Empty;

    [Required, Range(0.01, 10000)]
    [Display(Name = "Distance (km)")]
    public decimal? DistanceKm { get; set; }

    [Required]
    [Display(Name = "Event type")]
    public string EventType { get; set; } = "Run";

    [Display(Name = "Banner image")]
    public IFormFile? Banner { get; set; }

    public string? ExistingBannerUrl { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (!new[] { "Run", "Walk", "Cycle" }.Contains(EventType))
            yield return new ValidationResult("Choose Run, Walk or Cycle.", new[] { nameof(EventType) });
    }

    public EventRequest ToRequest() => new(
        Name.Trim(),
        string.IsNullOrWhiteSpace(Description) ? null : Description.Trim(),
        EventDate!.Value,
        Location.Trim(),
        DistanceKm!.Value,
        EventType);
}

public class CategoryFormViewModel : IValidatableObject
{
    public int? CategoryId { get; set; }
    public int EventId { get; set; }
    public string EventName { get; set; } = string.Empty;

    [Required, StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [Range(0.01, 10000)]
    [Display(Name = "Distance (km)")]
    public decimal? DistanceKm { get; set; }

    [Range(0, 120)]
    [Display(Name = "Minimum age")]
    public int? MinAge { get; set; }

    [Range(0, 120)]
    [Display(Name = "Maximum age")]
    public int? MaxAge { get; set; }

    [Range(0, 100000)]
    public decimal Price { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (DistanceKm is null && MinAge is null && MaxAge is null)
            yield return new ValidationResult(
                "A category needs a distance, an age limit, or both.",
                new[] { nameof(DistanceKm), nameof(MinAge), nameof(MaxAge) });

        if (MinAge is not null && MaxAge is not null && MinAge > MaxAge)
            yield return new ValidationResult(
                "Minimum age cannot be greater than maximum age.",
                new[] { nameof(MinAge), nameof(MaxAge) });
    }

    public CategoryRequest ToRequest() => new(Name.Trim(), DistanceKm, MinAge, MaxAge, Price);
}
