using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using TVTracker.Models;

namespace TVTracker.Data;

public class ApplicationDbContext : IdentityDbContext<AppUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    public DbSet<Show> Shows => Set<Show>();
    public DbSet<Season> Seasons => Set<Season>();
    public DbSet<Episode> Episodes => Set<Episode>();
    public DbSet<UserShow> UserShows => Set<UserShow>();
    public DbSet<WatchedEpisode> WatchedEpisodes => Set<WatchedEpisode>();
    public DbSet<EpisodeRating> EpisodeRatings => Set<EpisodeRating>();
    public DbSet<ShowReview> ShowReviews => Set<ShowReview>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Show>(e =>
        {
            e.HasIndex(s => s.TmdbId).IsUnique().HasFilter("\"TmdbId\" IS NOT NULL");
            e.Property(s => s.AverageRating).HasPrecision(4, 2);
        });

        builder.Entity<Episode>(e =>
        {
            e.Property(ep => ep.AverageRating).HasPrecision(4, 2);
        });

        builder.Entity<UserShow>(e =>
        {
            e.HasIndex(u => new { u.UserId, u.ShowId }).IsUnique();
            e.Property(u => u.Status).HasConversion<string>();
        });

        builder.Entity<WatchedEpisode>(e =>
        {
            e.HasIndex(w => new { w.UserId, w.EpisodeId }).IsUnique();
        });

        builder.Entity<EpisodeRating>(e =>
        {
            e.HasIndex(r => new { r.UserId, r.EpisodeId }).IsUnique();
        });

        builder.Entity<ShowReview>(e =>
        {
            e.HasIndex(r => new { r.UserId, r.ShowId }).IsUnique();
        });
    }
}
