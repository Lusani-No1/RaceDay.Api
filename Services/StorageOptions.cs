namespace RaceDay.Api.Services;

/// <summary>Where uploaded images are stored. Configured under the "Storage" section.</summary>
public class StorageOptions
{
    /// <summary>"Local" (default) or "Azure".</summary>
    public string Provider { get; set; } = "Local";

    /// <summary>Folder under wwwroot used by the local provider.</summary>
    public string LocalPath { get; set; } = "uploads";

    /// <summary>Public URL prefix for locally stored files, e.g. "/uploads".</summary>
    public string PublicBaseUrl { get; set; } = "/uploads";

    public AzureBlobOptions Azure { get; set; } = new();
}

public class AzureBlobOptions
{
    /// <summary>Azure Storage connection string. Leave empty to use the local provider.</summary>
    public string? ConnectionString { get; set; }

    /// <summary>Blob container name.</summary>
    public string ContainerName { get; set; } = "raceday";

    /// <summary>
    /// Optional public base URL for the container (CDN or custom domain).
    /// When empty the container's blob URL is returned.
    /// </summary>
    public string? PublicBaseUrl { get; set; }
}
