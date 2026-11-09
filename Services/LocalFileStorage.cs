using Microsoft.Extensions.Options;

namespace RaceDay.Api.Services;

/// <summary>Stores images under wwwroot so they are served by UseStaticFiles (no Azure account needed).</summary>
public class LocalFileStorage : IFileStorage
{
    private readonly IWebHostEnvironment _env;
    private readonly StorageOptions _options;

    public LocalFileStorage(IWebHostEnvironment env, IOptions<StorageOptions> options)
    {
        _env = env;
        _options = options.Value;
    }

    public async Task<string> SaveAsync(Stream content, string fileName, string contentType,
        string folder, CancellationToken ct = default)
    {
        var root = string.IsNullOrWhiteSpace(_env.WebRootPath)
            ? Path.Combine(_env.ContentRootPath, "wwwroot")
            : _env.WebRootPath;

        var directory = Path.Combine(root, _options.LocalPath, folder);
        Directory.CreateDirectory(directory);

        var extension = Path.GetExtension(fileName);
        if (string.IsNullOrWhiteSpace(extension)) extension = ".img";

        var storedName = $"{Guid.NewGuid():N}{extension.ToLowerInvariant()}";
        var path = Path.Combine(directory, storedName);

        await using (var file = File.Create(path))
        {
            await content.CopyToAsync(file, ct);
        }

        return $"{_options.PublicBaseUrl.TrimEnd('/')}/{folder}/{storedName}";
    }

    public Task DeleteAsync(string? url, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(url)) return Task.CompletedTask;

        var prefix = $"{_options.PublicBaseUrl.TrimEnd('/')}/";
        if (!url.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) return Task.CompletedTask;

        var root = string.IsNullOrWhiteSpace(_env.WebRootPath)
            ? Path.Combine(_env.ContentRootPath, "wwwroot")
            : _env.WebRootPath;

        // URL is like /uploads/event-banners/<file>; map it below wwwroot.
        var relative = url.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
        var path = Path.GetFullPath(Path.Combine(root, relative));
        var allowedRoot = Path.GetFullPath(Path.Combine(root, _options.LocalPath));

        if (path.StartsWith(allowedRoot, StringComparison.OrdinalIgnoreCase) && File.Exists(path))
            File.Delete(path);

        return Task.CompletedTask;
    }
}
