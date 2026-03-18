using Microsoft.AspNetCore.Identity;

namespace TVTracker.Models;

public class AppUser : IdentityUser
{
    public string DisplayName { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public ICollection<UserShow> UserShows { get; set; } = new List<UserShow>();
    public ICollection<EpisodeRating> EpisodeRatings { get; set; } = new List<EpisodeRating>();
    public ICollection<ShowReview> ShowReviews { get; set; } = new List<ShowReview>();
}

public class Show
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? PosterUrl { get; set; }
    public string? BackdropUrl { get; set; }
    public string? Genre { get; set; }
    public string? Network { get; set; }
    public string Status { get; set; } = "Ongoing"; // Ongoing, Ended, Cancelled
    public int? TmdbId { get; set; }
    public double AverageRating { get; set; }
    public int RatingCount { get; set; }
    public DateTime? FirstAirDate { get; set; }
    public DateTime? LastAirDate { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public ICollection<Season> Seasons { get; set; } = new List<Season>();
    public ICollection<UserShow> UserShows { get; set; } = new List<UserShow>();
    public ICollection<ShowReview> Reviews { get; set; } = new List<ShowReview>();
}

public class Season
{
    public int Id { get; set; }
    public int ShowId { get; set; }
    public Show Show { get; set; } = null!;
    public int SeasonNumber { get; set; }
    public string? Title { get; set; }
    public string? Description { get; set; }
    public string? PosterUrl { get; set; }
    public DateTime? AirDate { get; set; }
    public ICollection<Episode> Episodes { get; set; } = new List<Episode>();
}

public class Episode
{
    public int Id { get; set; }
    public int SeasonId { get; set; }
    public Season Season { get; set; } = null!;
    public int EpisodeNumber { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int? DurationMinutes { get; set; }
    public DateTime? AirDate { get; set; }
    public string? ThumbnailUrl { get; set; }
    public double AverageRating { get; set; }
    public int RatingCount { get; set; }
    public ICollection<EpisodeRating> Ratings { get; set; } = new List<EpisodeRating>();
    public ICollection<WatchedEpisode> WatchedByUsers { get; set; } = new List<WatchedEpisode>();
}

public class UserShow
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public AppUser User { get; set; } = null!;
    public int ShowId { get; set; }
    public Show Show { get; set; } = null!;
    public WatchStatus Status { get; set; } = WatchStatus.PlanToWatch;
    public int? UserRating { get; set; } // 1-10
    public bool IsFavorite { get; set; }
    public DateTime AddedAt { get; set; } = DateTime.UtcNow;
    public DateTime? StartedAt { get; set; }
    public DateTime? FinishedAt { get; set; }
    public string? Notes { get; set; }
}

public enum WatchStatus
{
    Watching,
    Completed,
    OnHold,
    Dropped,
    PlanToWatch
}

public class WatchedEpisode
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public AppUser User { get; set; } = null!;
    public int EpisodeId { get; set; }
    public Episode Episode { get; set; } = null!;
    public DateTime WatchedAt { get; set; } = DateTime.UtcNow;
}

public class EpisodeRating
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public AppUser User { get; set; } = null!;
    public int EpisodeId { get; set; }
    public Episode Episode { get; set; } = null!;
    public int Rating { get; set; } // 1-10
    public string? Comment { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class ShowReview
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public AppUser User { get; set; } = null!;
    public int ShowId { get; set; }
    public Show Show { get; set; } = null!;
    public string Content { get; set; } = string.Empty;
    public int Rating { get; set; } // 1-10
    public bool ContainsSpoilers { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}
