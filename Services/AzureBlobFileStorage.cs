using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Options;

namespace RaceDay.Api.Services;

/// <summary>Stores images in an Azure Blob Storage container.</summary>
public class AzureBlobFileStorage : IFileStorage
{
    private readonly StorageOptions _options;
    private readonly BlobContainerClient _container;

    public AzureBlobFileStorage(IOptions<StorageOptions> options)
    {
        _options = options.Value;

        if (string.IsNullOrWhiteSpace(_options.Azure.ConnectionString))
            throw new InvalidOperationException(
                "Storage:Provider is 'Azure' but Storage:Azure:ConnectionString is empty.");

        _container = new BlobContainerClient(
            _options.Azure.ConnectionString, _options.Azure.ContainerName);
    }

    public async Task<string> SaveAsync(Stream content, string fileName, string contentType,
        string folder, CancellationToken ct = default)
    {
        await _container.CreateIfNotExistsAsync(PublicAccessType.Blob, cancellationToken: ct);

        var extension = Path.GetExtension(fileName);
        if (string.IsNullOrWhiteSpace(extension)) extension = ".img";

        var blobName = $"{folder}/{Guid.NewGuid():N}{extension.ToLowerInvariant()}";
        var blob = _container.GetBlobClient(blobName);

        await blob.UploadAsync(content, new BlobHttpHeaders { ContentType = contentType }, cancellationToken: ct);

        if (!string.IsNullOrWhiteSpace(_options.Azure.PublicBaseUrl))
            return $"{_options.Azure.PublicBaseUrl.TrimEnd('/')}/{blobName}";

        return blob.Uri.ToString();
    }

    public async Task DeleteAsync(string? url, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(url)) return;

        // Only delete blobs we produced; ignore external/signed URLs.
        var marker = $"/{_options.Azure.ContainerName}/";
        var index = url.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
        if (index < 0) return;

        var blobName = url[(index + marker.Length)..];
        var queryIndex = blobName.IndexOf('?');
        if (queryIndex >= 0) blobName = blobName[..queryIndex];

        if (!string.IsNullOrWhiteSpace(blobName))
            await _container.GetBlobClient(blobName).DeleteIfExistsAsync(cancellationToken: ct);
    }
}
