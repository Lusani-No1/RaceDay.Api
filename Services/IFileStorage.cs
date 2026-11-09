namespace RaceDay.Api.Services;

/// <summary>Stores uploaded images either in Azure Blob Storage or on the local file system.</summary>
public interface IFileStorage
{
    /// <summary>Saves an image and returns its public URL.</summary>
    Task<string> SaveAsync(Stream content, string fileName, string contentType, string folder,
        CancellationToken ct = default);

    /// <summary>Deletes a previously stored image, ignoring files that are not ours.</summary>
    Task DeleteAsync(string? url, CancellationToken ct = default);
}
