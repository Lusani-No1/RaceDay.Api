namespace RaceDay.Api.Models;

public class UserProfile
{
    public int ProfileId { get; set; }
    public int UserId { get; set; }
    public string? PhoneNumber { get; set; }
    public DateOnly? DateOfBirth { get; set; }
    public string? EmergencyContact { get; set; }
    public string? ProfileImageUrl { get; set; }

    public User User { get; set; } = null!;
}
