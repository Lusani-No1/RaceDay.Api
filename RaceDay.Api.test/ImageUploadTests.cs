using System.Net;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using RaceDay.Api.Services;

namespace RaceDay.Api.Tests;

/// <summary>
/// Same API and in-memory database as <see cref="RaceDayFactory"/>, but uploads go
/// to a temporary web root so tests never write into the repository.
/// </summary>
public class StorageTestFactory : RaceDayFactory
{
    public string WebRoot { get; } = Path.Combine(Path.GetTempPath(), "raceday-tests", Guid.NewGuid().ToString("N"));

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);

        Directory.CreateDirectory(WebRoot);
        builder.UseWebRoot(WebRoot);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        try
        {
            if (Directory.Exists(WebRoot)) Directory.Delete(WebRoot, recursive: true);
        }
        catch (IOException)
        {
            // A lingering file handle should not fail the test run.
        }
    }
}

/// <summary>Banner and profile-picture uploads through the real API endpoints.</summary>
public class ImageUploadTests : ApiTestBase, IClassFixture<StorageTestFactory>
{
    private readonly StorageTestFactory _factory;

    public ImageUploadTests(StorageTestFactory factory) : base(factory) => _factory = factory;

    private static MultipartFormDataContent ImageForm(string fileName, string contentType, int bytes = 64)
    {
        var content = new ByteArrayContent(new byte[bytes]);
        content.Headers.ContentType = MediaTypeHeaderValue.Parse(contentType);

        var form = new MultipartFormDataContent();
        form.Add(content, "file", fileName);
        return form;
    }

    private string PathFor(string url) =>
        Path.Combine(_factory.WebRoot, url.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));

    [Fact]
    public async Task Organiser_can_upload_an_event_banner_and_it_is_saved()
    {
        var organiser = await LoggedInClientAsync("Organiser");
        var eventId = await CreateEventAsync(organiser);

        using var form = ImageForm("banner.png", "image/png");
        var response = await organiser.PostAsync($"/api/events/{eventId}/banner", form);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var url = (await ReadJsonAsync(response)).GetProperty("bannerUrl").GetString();
        Assert.False(string.IsNullOrWhiteSpace(url));
        Assert.StartsWith("/uploads/event-banners/", url);
        Assert.True(File.Exists(PathFor(url!)), "The uploaded banner should exist in the web root.");

        // The URL is persisted, so a later read returns it too.
        var detail = await organiser.GetAsync($"/api/events/{eventId}");
        var saved = (await ReadJsonAsync(detail)).GetProperty("bannerUrl").GetString();
        Assert.Equal(url, saved);
    }

    [Fact]
    public async Task Uploading_a_new_banner_replaces_the_previous_file()
    {
        var organiser = await LoggedInClientAsync("Organiser");
        var eventId = await CreateEventAsync(organiser);

        using (var first = ImageForm("first.png", "image/png"))
        {
            var response = await organiser.PostAsync($"/api/events/{eventId}/banner", first);
            response.EnsureSuccessStatusCode();
        }

        var firstUrl = (await ReadJsonAsync(await organiser.GetAsync($"/api/events/{eventId}")))
            .GetProperty("bannerUrl").GetString()!;

        using (var second = ImageForm("second.jpg", "image/jpeg"))
        {
            var response = await organiser.PostAsync($"/api/events/{eventId}/banner", second);
            response.EnsureSuccessStatusCode();
        }

        var secondUrl = (await ReadJsonAsync(await organiser.GetAsync($"/api/events/{eventId}")))
            .GetProperty("bannerUrl").GetString()!;

        Assert.NotEqual(firstUrl, secondUrl);
        Assert.False(File.Exists(PathFor(firstUrl)), "The replaced banner should be deleted.");
        Assert.True(File.Exists(PathFor(secondUrl)));
    }

    [Theory]
    [InlineData("notes.txt", "text/plain")]
    [InlineData("photo.gif", "image/gif")]
    [InlineData("screenshot.bmp", "image/bmp")]
    public async Task Banner_upload_rejects_unsupported_file_types(string fileName, string contentType)
    {
        var organiser = await LoggedInClientAsync("Organiser");
        var eventId = await CreateEventAsync(organiser);

        using var form = ImageForm(fileName, contentType);
        var response = await organiser.PostAsync($"/api/events/{eventId}/banner", form);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Banner_upload_rejects_an_image_larger_than_five_megabytes()
    {
        var organiser = await LoggedInClientAsync("Organiser");
        var eventId = await CreateEventAsync(organiser);

        using var form = ImageForm("huge.png", "image/png", bytes: 5_000_001);
        var response = await organiser.PostAsync($"/api/events/{eventId}/banner", form);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Banner_upload_is_rejected_for_another_organisers_event()
    {
        var owner = await LoggedInClientAsync("Organiser");
        var eventId = await CreateEventAsync(owner);

        var intruder = await LoggedInClientAsync("Organiser");
        using var form = ImageForm("banner.png", "image/png");
        var response = await intruder.PostAsync($"/api/events/{eventId}/banner", form);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Banner_upload_is_rejected_for_a_participant()
    {
        var organiser = await LoggedInClientAsync("Organiser");
        var eventId = await CreateEventAsync(organiser);

        var participant = await LoggedInClientAsync("Participant");
        using var form = ImageForm("banner.png", "image/png");
        var response = await participant.PostAsync($"/api/events/{eventId}/banner", form);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Banner_upload_requires_a_session()
    {
        var organiser = await LoggedInClientAsync("Organiser");
        var eventId = await CreateEventAsync(organiser);

        using var form = ImageForm("banner.png", "image/png");
        var response = await NewClient().PostAsync($"/api/events/{eventId}/banner", form);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task User_can_upload_a_profile_picture()
    {
        var participant = await LoggedInClientAsync("Participant");

        using var form = ImageForm("me.webp", "image/webp");
        var response = await participant.PostAsync("/api/users/me/profile-picture", form);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var url = (await ReadJsonAsync(response)).GetProperty("profileImageUrl").GetString();
        Assert.False(string.IsNullOrWhiteSpace(url));
        Assert.StartsWith("/uploads/profile-pictures/", url);

        var me = await participant.GetAsync("/api/users/me");
        var saved = (await ReadJsonAsync(me)).GetProperty("profileImageUrl").GetString();
        Assert.Equal(url, saved);
        Assert.True(File.Exists(PathFor(url!)));
    }

    [Fact]
    public async Task Profile_picture_upload_rejects_a_pdf()
    {
        var participant = await LoggedInClientAsync("Participant");

        using var form = ImageForm("resume.pdf", "application/pdf");
        var response = await participant.PostAsync("/api/users/me/profile-picture", form);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}

/// <summary>Which storage provider the API picks from configuration.</summary>
public class StorageProviderTests
{
    private static IFileStorage Resolve(Dictionary<string, string?> settings)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(settings)
            .Build();

        var services = new ServiceCollection();
        services.AddSingleton<IWebHostEnvironment>(new TestWebHostEnvironment());
        services.AddRaceDayFileStorage(configuration);

        return services.BuildServiceProvider().GetRequiredService<IFileStorage>();
    }

    [Fact]
    public void Local_storage_is_used_by_default()
    {
        var storage = Resolve(new Dictionary<string, string?>());
        Assert.IsType<LocalFileStorage>(storage);
    }

    [Fact]
    public void Azure_storage_is_used_when_the_provider_is_configured()
    {
        var storage = Resolve(new Dictionary<string, string?>
        {
            ["Storage:Provider"] = "Azure",
            // Azurite's public development connection string; no request is made here.
            ["Storage:Azure:ConnectionString"] =
                "DefaultEndpointsProtocol=http;AccountName=devstoreaccount1;" +
                "AccountKey=Eby8vdM02xNOcqFlqUwJPLlmEtlCDXJ1OUzFT50uSRZ6IFsuFq2UVErCz4I6tq/K1SZFPTOtr/KBHBeksoGMGw==;" +
                "BlobEndpoint=http://127.0.0.1:10000/devstoreaccount1;",
            ["Storage:Azure:ContainerName"] = "raceday"
        });

        Assert.IsType<AzureBlobFileStorage>(storage);
    }

    [Fact]
    public void Azure_storage_fails_fast_without_a_connection_string()
    {
        var error = Assert.Throws<InvalidOperationException>(() => Resolve(new Dictionary<string, string?>
        {
            ["Storage:Provider"] = "Azure",
            ["Storage:Azure:ConnectionString"] = ""
        }));

        Assert.Contains("ConnectionString", error.Message);
    }

    private sealed class TestWebHostEnvironment : IWebHostEnvironment
    {
        public string ApplicationName { get; set; } = "RaceDay.Api.Tests";
        public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
        public string WebRootPath { get; set; } = Path.GetTempPath();
        public string EnvironmentName { get; set; } = "Development";
        public string ContentRootPath { get; set; } = Path.GetTempPath();
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
