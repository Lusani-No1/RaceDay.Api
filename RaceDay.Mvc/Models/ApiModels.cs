namespace RaceDay.Mvc.Models;

public record UserDto(int UserId, string FullName, string Email, string Role);

public record ProfileDto(
    int UserId,
    string FullName,
    string Email,
    string Role,
    string? PhoneNumber,
    DateOnly? DateOfBirth,
    string? EmergencyContact,
    string? ProfileImageUrl,
    DateTime CreatedAt);

public record EventDto(
    int EventId,
    int OrganiserId,
    string OrganiserName,
    string Name,
    string? Description,
    DateOnly EventDate,
    string Location,
    decimal DistanceKm,
    string EventType,
    string? BannerUrl);

public record EventDetailDto(
    int EventId,
    int OrganiserId,
    string OrganiserName,
    string Name,
    string? Description,
    DateOnly EventDate,
    string Location,
    decimal DistanceKm,
    string EventType,
    string? BannerUrl,
    IReadOnlyList<CategoryDto> Categories);

public record CategoryDto(
    int CategoryId,
    int EventId,
    string Name,
    decimal? DistanceKm,
    int? MinAge,
    int? MaxAge,
    decimal Price);

public record EnrolmentDto(
    int EnrolmentId,
    int ParticipantId,
    string ParticipantName,
    int CategoryId,
    string CategoryName,
    int EventId,
    string EventName,
    DateOnly EventDate,
    DateTime EnrolmentDate,
    string Status);

public record ResultDto(
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

// ---------- Request bodies sent to the API ----------

public record RegisterRequest(
    string FullName,
    string Email,
    string Password,
    string Role,
    string? OrganiserAccessCode);

public record LoginRequest(string Email, string Password);

public record EventRequest(
    string Name,
    string? Description,
    DateOnly EventDate,
    string Location,
    decimal DistanceKm,
    string EventType);

public record CategoryRequest(
    string Name,
    decimal? DistanceKm,
    int? MinAge,
    int? MaxAge,
    decimal Price);

public record ResultRequest(TimeSpan FinishTime, int Position);

public record UpdateProfileRequest(
    string FullName,
    string? PhoneNumber,
    DateOnly? DateOfBirth,
    string? EmergencyContact,
    string? ProfileImageUrl);
