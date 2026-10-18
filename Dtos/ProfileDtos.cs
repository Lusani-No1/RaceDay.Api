using System.ComponentModel.DataAnnotations;
using RaceDay.Api.Models;

namespace RaceDay.Api.Dtos;

public class UpdateProfileRequest
{
    /// <summary>Your full name.</summary>
    /// <example>Test Runner</example>
    [Required, StringLength(100)]
    public string FullName { get; set; } = string.Empty;

    /// <summary>Contact number.</summary>
    /// <example>0821234567</example>
    [StringLength(20)]
    public string? PhoneNumber { get; set; }

    /// <summary>Date of birth (yyyy-MM-dd). Cannot be in the future.</summary>
    /// <example>1995-03-14</example>
    public DateOnly? DateOfBirth { get; set; }

    /// <summary>Who to contact in an emergency.</summary>
    /// <example>Nomsa Nkosi - 0837654321</example>
    [StringLength(100)]
    public string? EmergencyContact { get; set; }

    /// <summary>Link to a profile picture.</summary>
    [StringLength(255)]
    public string? ProfileImageUrl { get; set; }
}

public record ProfileResponse(
    int UserId,
    string FullName,
    string Email,
    UserRole Role,
    string? PhoneNumber,
    DateOnly? DateOfBirth,
    string? EmergencyContact,
    string? ProfileImageUrl,
    DateTime CreatedAt);
