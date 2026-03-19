using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TVTracker.Data;
using TVTracker.DTOs;
using TVTracker.Models;

namespace TVTracker.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class EpisodesController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    public EpisodesController(ApplicationDbContext db) { _db = db; }
    private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    [HttpPost("{id}/watch")]
    public async Task<IActionResult> MarkWatched(int id)
    {
        var episode = await _db.Episodes.FindAsync(id);
        if (episode == null) return NotFound();

        var existing = await _db.WatchedEpisodes.FirstOrDefaultAsync(w => w.EpisodeId == id && w.UserId == UserId);
        if (existing == null)
        {
            _db.WatchedEpisodes.Add(new WatchedEpisode { UserId = UserId, EpisodeId = id });
            await _db.SaveChangesAsync();
        }
        return Ok(new { watched = true });
    }

    [HttpDelete("{id}/watch")]
    public async Task<IActionResult> UnmarkWatched(int id)
    {
        var watched = await _db.WatchedEpisodes.FirstOrDefaultAsync(w => w.EpisodeId == id && w.UserId == UserId);
        if (watched != null)
        {
            _db.WatchedEpisodes.Remove(watched);
            await _db.SaveChangesAsync();
        }
        return Ok(new { watched = false });
    }

    [HttpPost("{id}/rate")]
    public async Task<IActionResult> RateEpisode(int id, RateEpisodeDto dto)
    {
        var episode = await _db.Episodes.FindAsync(id);
        if (episode == null) return NotFound();

        var rating = await _db.EpisodeRatings.FirstOrDefaultAsync(r => r.EpisodeId == id && r.UserId == UserId);
        if (rating == null)
        {
            rating = new EpisodeRating { UserId = UserId, EpisodeId = id };
            _db.EpisodeRatings.Add(rating);
        }
        rating.Rating = dto.Rating;
        rating.Comment = dto.Comment;
        await _db.SaveChangesAsync();

        // Update episode average
        var ratings = await _db.EpisodeRatings.Where(r => r.EpisodeId == id).ToListAsync();
        episode.AverageRating = ratings.Average(r => r.Rating);
        episode.RatingCount = ratings.Count;
        await _db.SaveChangesAsync();

        return Ok(new { rating = dto.Rating });
    }
}

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UserController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    public UserController(ApplicationDbContext db) { _db = db; }
    private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    [HttpGet("list")]
    public async Task<ActionResult<List<UserShowDto>>> GetMyList([FromQuery] string? status)
    {
        var query = _db.UserShows
            .Where(u => u.UserId == UserId)
            .Include(u => u.Show).ThenInclude(s => s.Seasons).ThenInclude(s => s.Episodes)
            .AsQueryable();

        if (!string.IsNullOrEmpty(status) && Enum.TryParse<WatchStatus>(status, out var ws))
            query = query.Where(u => u.Status == ws);

        var userShows = await query.OrderByDescending(u => u.AddedAt).ToListAsync();
        var episodeIds = userShows.SelectMany(u => u.Show.Seasons.SelectMany(s => s.Episodes).Select(e => e.Id)).ToList();
        var watchedIds = (await _db.WatchedEpisodes
            .Where(w => w.UserId == UserId && episodeIds.Contains(w.EpisodeId))
            .Select(w => w.EpisodeId).ToListAsync()).ToHashSet();

        return Ok(userShows.Select(u => new UserShowDto
        {
            Id = u.Id,
            Show = new ShowDto
            {
                Id = u.Show.Id, Title = u.Show.Title, Description = u.Show.Description,
                PosterUrl = u.Show.PosterUrl, BackdropUrl = u.Show.BackdropUrl,
                Genre = u.Show.Genre, Network = u.Show.Network, Status = u.Show.Status,
                AverageRating = u.Show.AverageRating, RatingCount = u.Show.RatingCount,
                FirstAirDate = u.Show.FirstAirDate, LastAirDate = u.Show.LastAirDate,
                SeasonsCount = u.Show.Seasons.Count,
                EpisodesCount = u.Show.Seasons.Sum(s => s.Episodes.Count),
                UserStatus = u.Status.ToString(), UserRating = u.UserRating, IsFavorite = u.IsFavorite
            },
            Status = u.Status.ToString(),
            UserRating = u.UserRating,
            IsFavorite = u.IsFavorite,
            AddedAt = u.AddedAt,
            Notes = u.Notes,
            WatchedEpisodes = u.Show.Seasons.SelectMany(s => s.Episodes).Count(e => watchedIds.Contains(e.Id)),
            TotalEpisodes = u.Show.Seasons.Sum(s => s.Episodes.Count)
        }));
    }

    [HttpGet("stats")]
    public async Task<ActionResult<UserStatsDto>> GetStats()
    {
        var userShows = await _db.UserShows
            .Where(u => u.UserId == UserId)
            .Include(u => u.Show).ThenInclude(s => s.Seasons).ThenInclude(s => s.Episodes)
            .ToListAsync();

        var totalEpisodesWatched = await _db.WatchedEpisodes.CountAsync(w => w.UserId == UserId);
        var watchedEpisodeDetails = await _db.WatchedEpisodes
            .Where(w => w.UserId == UserId)
            .Include(w => w.Episode)
            .ToListAsync();

        var totalMinutes = watchedEpisodeDetails.Sum(w => w.Episode?.DurationMinutes ?? 45);
        var ratings = await _db.EpisodeRatings.Where(r => r.UserId == UserId).ToListAsync();

        var recentShows = await _db.UserShows
            .Where(u => u.UserId == UserId)
            .OrderByDescending(u => u.AddedAt)
            .Take(5)
            .Include(u => u.Show)
            .Select(u => u.Show)
            .ToListAsync();

        var favorites = await _db.UserShows
            .Where(u => u.UserId == UserId && u.IsFavorite)
            .Include(u => u.Show)
            .Select(u => u.Show)
            .ToListAsync();

        return Ok(new UserStatsDto
        {
            TotalShows = userShows.Count,
            WatchingShows = userShows.Count(u => u.Status == WatchStatus.Watching),
            CompletedShows = userShows.Count(u => u.Status == WatchStatus.Completed),
            PlanToWatchShows = userShows.Count(u => u.Status == WatchStatus.PlanToWatch),
            TotalEpisodesWatched = totalEpisodesWatched,
            TotalMinutesWatched = totalMinutes,
            AverageRating = ratings.Count > 0 ? ratings.Average(r => r.Rating) : 0,
            RecentlyWatched = recentShows.Select(s => new ShowDto { Id = s.Id, Title = s.Title, PosterUrl = s.PosterUrl, AverageRating = s.AverageRating }).ToList(),
            Favorites = favorites.Select(s => new ShowDto { Id = s.Id, Title = s.Title, PosterUrl = s.PosterUrl, AverageRating = s.AverageRating }).ToList()
        });
    }
}
