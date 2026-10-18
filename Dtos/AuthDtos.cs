using System.ComponentModel.DataAnnotations;
using RaceDay.Api.Models;

namespace RaceDay.Api.Dtos;

public class RegisterRequest
{
    /// <summary>Your full name.</summary>
    /// <example>Test Runner</example>
    [Required, StringLength(100)]
    public string FullName { get; set; } = string.Empty;

    /// <summary>Email address used to log in. Must be unique.</summary>
    /// <example>runner@test.com</example>
    [Required, EmailAddress, StringLength(150)]
    public string Email { get; set; } = string.Empty;

    /// <summary>At least 8 characters. It is stored hashed, never as plain text.</summary>
    /// <example>Password123</example>
    [Required, StringLength(100, MinimumLength = 8)]
    public string Password { get; set; } = string.Empty;

    /// <summary>Organiser or Participant.</summary>
    /// <example>Participant</example>
    [Required, EnumDataType(typeof(UserRole))]
    public UserRole? Role { get; set; }

    /// <summary>Required only when Role is Organiser. Leave out for Participants.</summary>
    public string? OrganiserAccessCode { get; set; }
}

public class LoginRequest
{
    /// <summary>The email used to register.</summary>
    /// <example>runner@test.com</example>
    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    /// <summary>The account password.</summary>
    /// <example>Password123</example>
    [Required]
    public string Password { get; set; } = string.Empty;
}

public record UserResponse(int UserId, string FullName, string Email, UserRole Role);
