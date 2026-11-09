using System.ComponentModel.DataAnnotations;
using RaceDay.Mvc.Models;

namespace RaceDay.Mvc.ViewModels;

public class OrganiserDashboardViewModel
{
    public IReadOnlyList<OrganiserEventSummary> Events { get; set; } = Array.Empty<OrganiserEventSummary>();
    public int TotalEvents => Events.Count;
    public int TotalEnrolments => Events.Sum(e => e.EnrolmentCount);
    public int UpcomingEvents => Events.Count(e => e.Event.EventDate >= DateOnly.FromDateTime(DateTime.Today));
}

public record OrganiserEventSummary(EventDto Event, int EnrolmentCount);

public class EventEnrolmentsViewModel
{
    public EventDto Event { get; set; } = null!;
    public IReadOnlyList<EnrolmentDto> Enrolments { get; set; } = Array.Empty<EnrolmentDto>();
}

public class EventResultsViewModel
{
    public EventDto Event { get; set; } = null!;
    public IReadOnlyList<ResultDto> Results { get; set; } = Array.Empty<ResultDto>();
    public IReadOnlyList<EnrolmentDto> AwaitingResults { get; set; } = Array.Empty<EnrolmentDto>();
}

public class ResultFormViewModel
{
    public int EventId { get; set; }
    public string EventName { get; set; } = string.Empty;

    public int EnrolmentId { get; set; }
    public string ParticipantName { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;

    public int? ResultId { get; set; }

    [Required]
    [Display(Name = "Finish time")]
    public TimeSpan? FinishTime { get; set; }

    [Required, Range(1, 100000)]
    public int? Position { get; set; }
}
