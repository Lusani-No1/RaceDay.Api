using System.ComponentModel.DataAnnotations;
using RaceDay.Mvc.Models;

namespace RaceDay.Mvc.ViewModels;

public class ProfileViewModel
{
    public ProfileDto Profile { get; set; } = null!;
}

public class EditProfileViewModel
{
    [Required, StringLength(100)]
    [Display(Name = "Full name")]
    public string FullName { get; set; } = string.Empty;

    [StringLength(20)]
    [Display(Name = "Phone number")]
    public string? PhoneNumber { get; set; }

    [DataType(DataType.Date)]
    [Display(Name = "Date of birth")]
    public DateOnly? DateOfBirth { get; set; }

    [StringLength(100)]
    [Display(Name = "Emergency contact")]
    public string? EmergencyContact { get; set; }

    [StringLength(255)]
    [Display(Name = "Profile picture URL")]
    public string? ProfileImageUrl { get; set; }

    public UpdateProfileRequest ToRequest() => new(
        FullName.Trim(), PhoneNumber, DateOfBirth, EmergencyContact, ProfileImageUrl);
}
