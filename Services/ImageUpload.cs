namespace RaceDay.Api.Services;

/// <summary>Shared validation for uploaded images.</summary>
public static class ImageUpload
{
    public const long MaxBytes = 5_000_000; // 5 MB

    private static readonly HashSet<string> AllowedContentTypes =
        new(StringComparer.OrdinalIgnoreCase) { "image/jpeg", "image/png", "image/webp" };

    private static readonly HashSet<string> AllowedExtensions =
        new(StringComparer.OrdinalIgnoreCase) { ".jpg", ".jpeg", ".png", ".webp" };

    /// <summary>Returns an error message, or null when the file is an acceptable image.</summary>
    public static string? Validate(IFormFile? file)
    {
        if (file is null || file.Length == 0)
            return "Please choose an image file.";

        if (file.Length > MaxBytes)
            return "The image must be 5 MB or smaller.";

        if (file.ContentType is null || !AllowedContentTypes.Contains(file.ContentType))
            return "Only JPEG, PNG or WebP images are allowed.";

        var extension = Path.GetExtension(file.FileName);
        if (string.IsNullOrWhiteSpace(extension) || !AllowedExtensions.Contains(extension))
            return "Only .jpg, .jpeg, .png or .webp files are allowed.";

        return null;
    }
}
