using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Models;

namespace RaceDay.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<UserProfile> UserProfiles => Set<UserProfile>();
    public DbSet<Event> Events => Set<Event>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Enrolment> Enrolments => Set<Enrolment>();
    public DbSet<Result> Results => Set<Result>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        // Users
        b.Entity<User>(e =>
        {
            e.HasKey(x => x.UserId);
            e.Property(x => x.FullName).HasMaxLength(100).IsRequired();
            e.Property(x => x.Email).HasMaxLength(150).IsRequired();
            e.HasIndex(x => x.Email).IsUnique();                       // UQ_Users_Email
            e.Property(x => x.PasswordHash).HasMaxLength(255).IsRequired();
            e.Property(x => x.Role).HasConversion<string>().HasMaxLength(20);
            e.Property(x => x.CreatedAt).HasDefaultValueSql("SYSDATETIME()");
            e.ToTable(t => t.HasCheckConstraint("CK_Users_Role", "[Role] IN ('Organiser','Participant')"));
        });

        // UserProfiles (1:1 with Users)
        b.Entity<UserProfile>(e =>
        {
            e.HasKey(x => x.ProfileId);
            e.Property(x => x.PhoneNumber).HasMaxLength(20);
            e.Property(x => x.EmergencyContact).HasMaxLength(100);
            e.Property(x => x.ProfileImageUrl).HasMaxLength(255);
            e.HasIndex(x => x.UserId).IsUnique();                      // UQ_UserProfiles_UserId
            e.HasOne(x => x.User).WithOne(u => u.Profile)
                .HasForeignKey<UserProfile>(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Events
        b.Entity<Event>(e =>
        {
            e.HasKey(x => x.EventId);
            e.Property(x => x.Name).HasMaxLength(150).IsRequired();
            e.Property(x => x.Description).HasMaxLength(1000);
            e.Property(x => x.Location).HasMaxLength(150).IsRequired();
            e.Property(x => x.DistanceKm).HasPrecision(6, 2);
            e.Property(x => x.EventType).HasConversion<string>().HasMaxLength(10);
            e.Property(x => x.BannerUrl).HasMaxLength(500);
            e.Property(x => x.CreatedAt).HasDefaultValueSql("SYSDATETIME()");
            e.HasOne(x => x.Organiser).WithMany(u => u.OrganisedEvents)
                .HasForeignKey(x => x.OrganiserId)
                .OnDelete(DeleteBehavior.Restrict);
            e.ToTable(t =>
            {
                t.HasCheckConstraint("CK_Events_Type", "[EventType] IN ('Run','Walk','Cycle')");
                t.HasCheckConstraint("CK_Events_Distance", "[DistanceKm] > 0");
            });
        });

        // Categories
        b.Entity<Category>(e =>
        {
            e.HasKey(x => x.CategoryId);
            e.Property(x => x.Name).HasMaxLength(100).IsRequired();
            e.Property(x => x.DistanceKm).HasPrecision(6, 2);
            e.Property(x => x.Price).HasPrecision(8, 2).HasDefaultValue(0m);
            e.HasOne(x => x.Event).WithMany(ev => ev.Categories)
                .HasForeignKey(x => x.EventId)
                .OnDelete(DeleteBehavior.Cascade);
            e.ToTable(t =>
            {
                t.HasCheckConstraint("CK_Categories_Basis",
                    "[DistanceKm] IS NOT NULL OR [MinAge] IS NOT NULL OR [MaxAge] IS NOT NULL");
                t.HasCheckConstraint("CK_Categories_AgeRange",
                    "[MinAge] IS NULL OR [MaxAge] IS NULL OR [MinAge] <= [MaxAge]");
            });
        });

        // Enrolments (junction: Participant <-> Category)
        b.Entity<Enrolment>(e =>
        {
            e.HasKey(x => x.EnrolmentId);
            e.Property(x => x.EnrolmentDate).HasDefaultValueSql("SYSDATETIME()");
            e.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
            e.HasIndex(x => new { x.ParticipantId, x.CategoryId }).IsUnique();
            e.HasOne(x => x.Participant).WithMany(u => u.Enrolments)
                .HasForeignKey(x => x.ParticipantId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Category).WithMany(c => c.Enrolments)
                .HasForeignKey(x => x.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);
            e.ToTable(t => t.HasCheckConstraint("CK_Enrolments_Status",
                "[Status] IN ('Pending','Confirmed','Cancelled')"));
        });

        // Results (1:1 with Enrolments)
        b.Entity<Result>(e =>
        {
            e.HasKey(x => x.ResultId);
            e.Property(x => x.FinishTime).HasColumnType("time(0)");
            e.HasIndex(x => x.EnrolmentId).IsUnique();                 // UQ_Results_EnrolmentId
            e.HasOne(x => x.Enrolment).WithOne(en => en.Result)
                .HasForeignKey<Result>(x => x.EnrolmentId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.CapturedBy).WithMany(u => u.CapturedResults)
                .HasForeignKey(x => x.CapturedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
            e.ToTable(t => t.HasCheckConstraint("CK_Results_Position", "[Position] > 0"));
        });
    }
}
