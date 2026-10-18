using System.ComponentModel.DataAnnotations;

namespace RaceDay.Api.Dtos;

public class CategoryRequest : IValidatableObject
{
    /// <summary>Name of the category.</summary>
    /// <example>Under 20</example>
    [Required, StringLength(100)]
    public string Name { get; set; } = string.Empty;

    /// <summary>Distance for distance-based categories, e.g. 10 or 21. Leave out for age-only categories.</summary>
    [Range(0.01, 10000)]
    public decimal? DistanceKm { get; set; }

    /// <summary>Minimum age for age-based categories, e.g. 60 for Senior.</summary>
    [Range(0, 120)]
    public int? MinAge { get; set; }

    /// <summary>Maximum age for age-based categories, e.g. 19 for Under 20.</summary>
    /// <example>19</example>
    [Range(0, 120)]
    public int? MaxAge { get; set; }

    /// <summary>Entry fee in rand.</summary>
    /// <example>100</example>
    [Range(0, 100000)]
    public decimal Price { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (DistanceKm is null && MinAge is null && MaxAge is null)
        {
            yield return new ValidationResult(
                "A category needs a distance, a minimum age, a maximum age, or a combination.",
                new[] { nameof(DistanceKm), nameof(MinAge), nameof(MaxAge) });
        }

        if (MinAge is not null && MaxAge is not null && MinAge > MaxAge)
        {
            yield return new ValidationResult(
                "MinAge cannot be greater than MaxAge.",
                new[] { nameof(MinAge), nameof(MaxAge) });
        }
    }
}

public record CategoryResponse(
    int CategoryId,
    int EventId,
    string Name,
    decimal? DistanceKm,
    int? MinAge,
    int? MaxAge,
    decimal Price);
