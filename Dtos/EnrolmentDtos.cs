using RaceDay.Api.Models;

namespace RaceDay.Api.Dtos;

public record EnrolmentResponse(
    int EnrolmentId,
    int ParticipantId,
    string ParticipantName,
    int CategoryId,
    string CategoryName,
    int EventId,
    string EventName,
    DateOnly EventDate,
    DateTime EnrolmentDate,
    EnrolmentStatus Status);
