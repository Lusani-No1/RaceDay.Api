using System.Reflection;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Models;
using RaceDay.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// Controllers; enums (Role, EventType...) are sent/received as text, e.g. "Organiser"
builder.Services.AddControllers()
    .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    // Shows the /// comments (summary, example values, response descriptions) in the Swagger page
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        options.IncludeXmlComments(xmlPath);
    }
});

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Password hashing (salted, one-way) - passwords are never stored in plain text
builder.Services.AddSingleton<IPasswordHasher<User>, PasswordHasher<User>>();

// Uploaded images: Azure Blob Storage when configured, otherwise local wwwroot/uploads.
builder.Services.Configure<StorageOptions>(builder.Configuration.GetSection("Storage"));
var storageProvider = builder.Configuration["Storage:Provider"] ?? "Local";
if (storageProvider.Equals("Azure", StringComparison.OrdinalIgnoreCase))
    builder.Services.AddSingleton<IFileStorage, AzureBlobFileStorage>();
else
    builder.Services.AddSingleton<IFileStorage, LocalFileStorage>();

// Session management: login issues an encrypted cookie holding the user's id and role
builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = "RaceDay.Session";
        options.Cookie.HttpOnly = true;
        options.ExpireTimeSpan = TimeSpan.FromHours(2);
        options.SlidingExpiration = true;

        // This is an API: answer 401/403 instead of redirecting to a login page
        options.Events.OnRedirectToLogin = ctx =>
        {
            ctx.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return Task.CompletedTask;
        };
        options.Events.OnRedirectToAccessDenied = ctx =>
        {
            ctx.Response.StatusCode = StatusCodes.Status403Forbidden;
            return Task.CompletedTask;
        };
    });

builder.Services.AddAuthorization();

var app = builder.Build();

// Docker/compose can wait for SQL Server this way instead of needing a manual Update-Database.
if (app.Configuration.GetValue("Database:MigrateOnStartup", false))
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

    for (var attempt = 1; ; attempt++)
    {
        try
        {
            await db.Database.MigrateAsync();
            break;
        }
        catch (Exception ex) when (attempt < 30)
        {
            app.Logger.LogWarning(ex, "Database is not ready (attempt {Attempt}/30); retrying in 3 seconds.", attempt);
            await Task.Delay(TimeSpan.FromSeconds(3));
        }
    }
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseStaticFiles();      // serves locally stored uploads
app.UseAuthentication();   // must come before UseAuthorization
app.UseAuthorization();
app.MapControllers();

app.Run();

// Lets the unit/integration test project start this app (used in stage 6)
public partial class Program { }
