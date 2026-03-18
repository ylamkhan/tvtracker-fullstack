using TVTracker.Models;

namespace TVTracker.DTOs;

// Auth DTOs
public record RegisterDto(string Email, string Password, string DisplayName);
public record LoginDto(string Email, string Password);
public record AuthResponseDto(string Token, string RefreshToken, UserDto User);

public class UserDto
{
    public string Id { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    public DateTime CreatedAt { get; set; }
}

// Show DTOs
public class ShowDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? PosterUrl { get; set; }
    public string? BackdropUrl { get; set; }
    public string? Genre { get; set; }
    public string? Network { get; set; }
    public string Status { get; set; } = string.Empty;
    public double AverageRating { get; set; }
    public int RatingCount { get; set; }
    public DateTime? FirstAirDate { get; set; }
    public DateTime? LastAirDate { get; set; }
    public int SeasonsCount { get; set; }
    public int EpisodesCount { get; set; }
    public string? UserStatus { get; set; } // Current user's watch status
    public int? UserRating { get; set; }
    public bool IsFavorite { get; set; }
}

public class ShowDetailDto : ShowDto
{
    public List<SeasonDto> Seasons { get; set; } = new();
    public List<ShowReviewDto> Reviews { get; set; } = new();
}

public class CreateShowDto
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? PosterUrl { get; set; }
    public string? BackdropUrl { get; set; }
    public string? Genre { get; set; }
    public string? Network { get; set; }
    public string Status { get; set; } = "Ongoing";
    public int? TmdbId { get; set; }
    public DateTime? FirstAirDate { get; set; }
}

// Season DTOs
public class SeasonDto
{
    public int Id { get; set; }
    public int ShowId { get; set; }
    public int SeasonNumber { get; set; }
    public string? Title { get; set; }
    public string? Description { get; set; }
    public string? PosterUrl { get; set; }
    public DateTime? AirDate { get; set; }
    public List<EpisodeDto> Episodes { get; set; } = new();
}

public class CreateSeasonDto
{
    public int ShowId { get; set; }
    public int SeasonNumber { get; set; }
    public string? Title { get; set; }
    public string? Description { get; set; }
    public DateTime? AirDate { get; set; }
}

// Episode DTOs
public class EpisodeDto
{
    public int Id { get; set; }
    public int SeasonId { get; set; }
    public int EpisodeNumber { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int? DurationMinutes { get; set; }
    public DateTime? AirDate { get; set; }
    public string? ThumbnailUrl { get; set; }
    public double AverageRating { get; set; }
    public int RatingCount { get; set; }
    public bool IsWatched { get; set; } // For current user
    public int? UserRating { get; set; }
}

public class CreateEpisodeDto
{
    public int SeasonId { get; set; }
    public int EpisodeNumber { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int? DurationMinutes { get; set; }
    public DateTime? AirDate { get; set; }
}

// UserShow DTOs
public class UserShowDto
{
    public int Id { get; set; }
    public ShowDto Show { get; set; } = null!;
    public string Status { get; set; } = string.Empty;
    public int? UserRating { get; set; }
    public bool IsFavorite { get; set; }
    public DateTime AddedAt { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? FinishedAt { get; set; }
    public string? Notes { get; set; }
    public int WatchedEpisodes { get; set; }
    public int TotalEpisodes { get; set; }
}

public class UpdateUserShowDto
{
    public string? Status { get; set; }
    public int? UserRating { get; set; }
    public bool? IsFavorite { get; set; }
    public string? Notes { get; set; }
}

// Rating DTOs
public class RateEpisodeDto
{
    public int Rating { get; set; } // 1-10
    public string? Comment { get; set; }
}

// Review DTOs
public class ShowReviewDto
{
    public int Id { get; set; }
    public UserDto User { get; set; } = null!;
    public string Content { get; set; } = string.Empty;
    public int Rating { get; set; }
    public bool ContainsSpoilers { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateReviewDto
{
    public string Content { get; set; } = string.Empty;
    public int Rating { get; set; }
    public bool ContainsSpoilers { get; set; }
}

// Stats DTO
public class UserStatsDto
{
    public int TotalShows { get; set; }
    public int WatchingShows { get; set; }
    public int CompletedShows { get; set; }
    public int PlanToWatchShows { get; set; }
    public int TotalEpisodesWatched { get; set; }
    public int TotalMinutesWatched { get; set; }
    public double AverageRating { get; set; }
    public List<ShowDto> RecentlyWatched { get; set; } = new();
    public List<ShowDto> Favorites { get; set; } = new();
}

public class PagedResult<T>
{
    public List<T> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
}
