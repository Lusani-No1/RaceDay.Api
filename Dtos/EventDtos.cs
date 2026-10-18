using System.ComponentModel.DataAnnotations;
using RaceDay.Api.Models;

namespace RaceDay.Api.Dtos;

public class EventRequest
{
    /// <summary>Name of the event.</summary>
    /// <example>Joburg City 10km</example>
    [Required, StringLength(150)]
    public string Name { get; set; } = string.Empty;

    /// <summary>A short description of the event.</summary>
    /// <example>Annual road race through the Johannesburg CBD.</example>
    [StringLength(1000)]
    public string? Description { get; set; }

    /// <summary>Date of the event (yyyy-MM-dd).</summary>
    /// <example>2026-11-08</example>
    [Required]
    public DateOnly? EventDate { get; set; }

    /// <summary>Where the event takes place.</summary>
    /// <example>Johannesburg, Gauteng</example>
    [Required, StringLength(150)]
    public string Location { get; set; } = string.Empty;

    /// <summary>Total distance of the event in kilometres.</summary>
    /// <example>10</example>
    [Required, Range(0.01, 10000)]
    public decimal? DistanceKm { get; set; }

    /// <summary>Run, Walk or Cycle.</summary>
    /// <example>Run</example>
    [Required, EnumDataType(typeof(EventType))]
    public EventType? EventType { get; set; }
}

public record EventResponse(
    int EventId,
    int OrganiserId,
    string OrganiserName,
    string Name,
    string? Description,
    DateOnly EventDate,
    string Location,
    decimal DistanceKm,
    EventType EventType);

public record EventDetailResponse(
    int EventId,
    int OrganiserId,
    string OrganiserName,
    string Name,
    string? Description,
    DateOnly EventDate,
    string Location,
    decimal DistanceKm,
    EventType EventType,
    IReadOnlyList<CategoryResponse> Categories);
