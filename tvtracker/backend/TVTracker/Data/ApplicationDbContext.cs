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
            e.HasIndex(s => s.TmdbId).IsUnique();
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

        // Seed some popular TV shows
        builder.Entity<Show>().HasData(
            new Show { Id = 1, Title = "Breaking Bad", Description = "A chemistry teacher turned drug lord.", Genre = "Crime,Drama", Network = "AMC", Status = "Ended", TmdbId = 1396, AverageRating = 9.5, FirstAirDate = new DateTime(2008, 1, 20), LastAirDate = new DateTime(2013, 9, 29) },
            new Show { Id = 2, Title = "Game of Thrones", Description = "Noble families fight for the Iron Throne.", Genre = "Fantasy,Drama", Network = "HBO", Status = "Ended", TmdbId = 1399, AverageRating = 8.2, FirstAirDate = new DateTime(2011, 4, 17), LastAirDate = new DateTime(2019, 5, 19) },
            new Show { Id = 3, Title = "Stranger Things", Description = "Kids face supernatural events in Hawkins.", Genre = "Sci-Fi,Horror,Drama", Network = "Netflix", Status = "Ended", TmdbId = 66732, AverageRating = 8.7, FirstAirDate = new DateTime(2016, 7, 15) },
            new Show { Id = 4, Title = "The Crown", Description = "The reign of Queen Elizabeth II.", Genre = "Drama,History", Network = "Netflix", Status = "Ended", TmdbId = 65494, AverageRating = 8.1, FirstAirDate = new DateTime(2016, 11, 4) },
            new Show { Id = 5, Title = "Succession", Description = "A media dynasty fights for power.", Genre = "Drama", Network = "HBO", Status = "Ended", TmdbId = 76669, AverageRating = 8.8, FirstAirDate = new DateTime(2018, 6, 3) },
            new Show { Id = 6, Title = "The Last of Us", Description = "A smuggler escorting a girl across post-apocalyptic America.", Genre = "Drama,Action", Network = "HBO", Status = "Ongoing", TmdbId = 100088, AverageRating = 8.9, FirstAirDate = new DateTime(2023, 1, 15) },
            new Show { Id = 7, Title = "Wednesday", Description = "Wednesday Addams at Nevermore Academy.", Genre = "Mystery,Horror,Comedy", Network = "Netflix", Status = "Ongoing", TmdbId = 119051, AverageRating = 8.1, FirstAirDate = new DateTime(2022, 11, 23) },
            new Show { Id = 8, Title = "House of the Dragon", Description = "Prequel to Game of Thrones.", Genre = "Fantasy,Drama", Network = "HBO", Status = "Ongoing", TmdbId = 94997, AverageRating = 8.4, FirstAirDate = new DateTime(2022, 8, 21) }
        );
    }
}
