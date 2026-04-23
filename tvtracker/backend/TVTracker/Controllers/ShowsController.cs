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
public class ShowsController : ControllerBase
{
    private readonly ApplicationDbContext _db;

    public ShowsController(ApplicationDbContext db) { _db = db; }

    private string? UserId => User.FindFirstValue(ClaimTypes.NameIdentifier);

    [HttpGet]
    public async Task<ActionResult<PagedResult<ShowDto>>> GetShows(
        [FromQuery] string? search,
        [FromQuery] string? genre,
        [FromQuery] string? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var query = _db.Shows.AsQueryable();

        if (!string.IsNullOrEmpty(search))
            query = query.Where(s => s.Title.ToLower().Contains(search.ToLower()));
        if (!string.IsNullOrEmpty(genre))
            query = query.Where(s => s.Genre != null && s.Genre.Contains(genre));
        if (!string.IsNullOrEmpty(status))
            query = query.Where(s => s.Status == status);

        var total = await query.CountAsync();
        var shows = await query
            .OrderByDescending(s => s.AverageRating)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Include(s => s.Seasons).ThenInclude(s => s.Episodes)
            .ToListAsync();

        return Ok(new PagedResult<ShowDto>
        {
            Items = shows.Select(s => MapShow(s)).ToList(),
            TotalCount = total,
            Page = page,
            PageSize = pageSize
        });
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ShowDetailDto>> GetShow(int id)
    {
        var show = await _db.Shows
            .Include(s => s.Seasons).ThenInclude(s => s.Episodes)
            .Include(s => s.Reviews).ThenInclude(r => r.User)
            .FirstOrDefaultAsync(s => s.Id == id);

        if (show == null) return NotFound();

        UserShow? userShow = null;
        if (UserId != null)
            userShow = await _db.UserShows.FirstOrDefaultAsync(u => u.ShowId == id && u.UserId == UserId);

        var watchedEpisodes = UserId != null
            ? await _db.WatchedEpisodes.Where(w => w.UserId == UserId).Select(w => w.EpisodeId).ToListAsync()
            : new List<int>();

        var userRatings = UserId != null
            ? await _db.EpisodeRatings.Where(r => r.UserId == UserId).ToDictionaryAsync(r => r.EpisodeId, r => r.Rating)
            : new Dictionary<int, int>();

        var dto = new ShowDetailDto
        {
            Id = show.Id,
            Title = show.Title,
            Description = show.Description,
            PosterUrl = show.PosterUrl,
            BackdropUrl = show.BackdropUrl,
            Genre = show.Genre,
            Network = show.Network,
            Status = show.Status,
            AverageRating = show.AverageRating,
            RatingCount = show.RatingCount,
            FirstAirDate = show.FirstAirDate,
            LastAirDate = show.LastAirDate,
            SeasonsCount = show.Seasons.Count,
            EpisodesCount = show.Seasons.Sum(s => s.Episodes.Count),
            UserStatus = userShow?.Status.ToString(),
            UserRating = userShow?.UserRating,
            IsFavorite = userShow?.IsFavorite ?? false,
            Seasons = show.Seasons.OrderBy(s => s.SeasonNumber).Select(season => new SeasonDto
            {
                Id = season.Id,
                ShowId = season.ShowId,
                SeasonNumber = season.SeasonNumber,
                Title = season.Title,
                Description = season.Description,
                PosterUrl = season.PosterUrl,
                AirDate = season.AirDate,
                Episodes = season.Episodes.OrderBy(e => e.EpisodeNumber).Select(ep => new EpisodeDto
                {
                    Id = ep.Id,
                    SeasonId = ep.SeasonId,
                    EpisodeNumber = ep.EpisodeNumber,
                    Title = ep.Title,
                    Description = ep.Description,
                    DurationMinutes = ep.DurationMinutes,
                    AirDate = ep.AirDate,
                    ThumbnailUrl = ep.ThumbnailUrl,
                    AverageRating = ep.AverageRating,
                    RatingCount = ep.RatingCount,
                    IsWatched = watchedEpisodes.Contains(ep.Id),
                    UserRating = userRatings.TryGetValue(ep.Id, out var r) ? r : null
                }).ToList()
            }).ToList(),
            Reviews = show.Reviews.OrderByDescending(r => r.CreatedAt).Select(r => new ShowReviewDto
            {
                Id = r.Id,
                User = new UserDto { Id = r.User.Id, DisplayName = r.User.DisplayName, AvatarUrl = r.User.AvatarUrl, Email = r.User.Email!, CreatedAt = r.User.CreatedAt },
                Content = r.Content,
                Rating = r.Rating,
                ContainsSpoilers = r.ContainsSpoilers,
                CreatedAt = r.CreatedAt
            }).ToList()
        };

        return Ok(dto);
    }

    [Authorize]
    [HttpPost]
    public async Task<ActionResult<ShowDto>> CreateShow(CreateShowDto dto)
    {
        var show = new Show
        {
            Title = dto.Title,
            Description = dto.Description,
            PosterUrl = dto.PosterUrl,
            BackdropUrl = dto.BackdropUrl,
            Genre = dto.Genre,
            Network = dto.Network,
            Status = dto.Status,
            TmdbId = dto.TmdbId,
            FirstAirDate = dto.FirstAirDate
        };
        _db.Shows.Add(show);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetShow), new { id = show.Id }, MapShow(show));
    }

    [Authorize]
    [HttpPost("{id}/track")]
    public async Task<ActionResult<UserShowDto>> TrackShow(int id, [FromBody] UpdateUserShowDto dto)
    {
        if (UserId == null) return Unauthorized();
    
        var show = await _db.Shows
            .Include(s => s.Seasons)
            .ThenInclude(s => s.Episodes)
            .FirstOrDefaultAsync(s => s.Id == id);
    
        if (show == null) return NotFound();
    
        var userShow = await _db.UserShows
            .FirstOrDefaultAsync(u => u.ShowId == id && u.UserId == UserId);
    
        if (userShow == null)
        {
            userShow = new UserShow { UserId = UserId, ShowId = id };
            _db.UserShows.Add(userShow);
        }
    
        if (dto.Status != null && Enum.TryParse<WatchStatus>(dto.Status, out var status))
            userShow.Status = status;
    
        if (dto.UserRating.HasValue)
            userShow.UserRating = dto.UserRating;
    
        if (dto.IsFavorite.HasValue)
            userShow.IsFavorite = dto.IsFavorite.Value;
    
        if (dto.Notes != null)
            userShow.Notes = dto.Notes;
    
        await _db.SaveChangesAsync();
    
        // ✅ FIXED PART
        var episodeIds = show.Seasons
            .SelectMany(s => s.Episodes)
            .Select(e => e.Id)
            .ToList();
    
        var watched = await _db.WatchedEpisodes.CountAsync(w =>
            w.UserId == UserId &&
            episodeIds.Contains(w.EpisodeId));
    
        return Ok(new UserShowDto
        {
            Id = userShow.Id,
            Show = MapShow(show),
            Status = userShow.Status.ToString(),
            UserRating = userShow.UserRating,
            IsFavorite = userShow.IsFavorite,
            AddedAt = userShow.AddedAt,
            Notes = userShow.Notes,
            WatchedEpisodes = watched,
            TotalEpisodes = show.Seasons.Sum(s => s.Episodes.Count)
        });
    }

    [Authorize]
    [HttpDelete("{id}/track")]
    public async Task<IActionResult> UntrackShow(int id)
    {
        var userShow = await _db.UserShows.FirstOrDefaultAsync(u => u.ShowId == id && u.UserId == UserId);
        if (userShow == null) return NotFound();
        _db.UserShows.Remove(userShow);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpGet("{id}/reviews")]
    public async Task<ActionResult<List<ShowReviewDto>>> GetReviews(int id)
    {
        var reviews = await _db.ShowReviews
            .Where(r => r.ShowId == id)
            .Include(r => r.User)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();
        return Ok(reviews.Select(r => new ShowReviewDto
        {
            Id = r.Id,
            User = new UserDto { Id = r.User.Id, DisplayName = r.User.DisplayName, AvatarUrl = r.User.AvatarUrl, Email = r.User.Email!, CreatedAt = r.User.CreatedAt },
            Content = r.Content,
            Rating = r.Rating,
            ContainsSpoilers = r.ContainsSpoilers,
            CreatedAt = r.CreatedAt
        }));
    }

    [Authorize]
    [HttpPost("{id}/reviews")]
    public async Task<ActionResult<ShowReviewDto>> CreateReview(int id, CreateReviewDto dto)
    {
        var existing = await _db.ShowReviews.FirstOrDefaultAsync(r => r.ShowId == id && r.UserId == UserId);
        if (existing != null) return BadRequest(new { message = "You already reviewed this show" });

        var review = new ShowReview
        {
            UserId = UserId!,
            ShowId = id,
            Content = dto.Content,
            Rating = dto.Rating,
            ContainsSpoilers = dto.ContainsSpoilers
        };
        _db.ShowReviews.Add(review);
        await _db.SaveChangesAsync();

        await UpdateShowRating(id);

        var user = await _db.Users.FindAsync(UserId);
        return Ok(new ShowReviewDto
        {
            Id = review.Id,
            User = new UserDto { Id = user!.Id, DisplayName = user.DisplayName, AvatarUrl = user.AvatarUrl, Email = user.Email!, CreatedAt = user.CreatedAt },
            Content = review.Content,
            Rating = review.Rating,
            ContainsSpoilers = review.ContainsSpoilers,
            CreatedAt = review.CreatedAt
        });
    }

    private async Task UpdateShowRating(int showId)
    {
        var reviews = await _db.ShowReviews.Where(r => r.ShowId == showId).ToListAsync();
        var show = await _db.Shows.FindAsync(showId);
        if (show != null && reviews.Count > 0)
        {
            show.AverageRating = reviews.Average(r => r.Rating);
            show.RatingCount = reviews.Count;
            await _db.SaveChangesAsync();
        }
    }

    private static ShowDto MapShow(Show show, UserShow? userShow = null) => new()
    {
        Id = show.Id,
        Title = show.Title,
        Description = show.Description,
        PosterUrl = show.PosterUrl,
        BackdropUrl = show.BackdropUrl,
        Genre = show.Genre,
        Network = show.Network,
        Status = show.Status,
        AverageRating = show.AverageRating,
        RatingCount = show.RatingCount,
        FirstAirDate = show.FirstAirDate,
        LastAirDate = show.LastAirDate,
        SeasonsCount = show.Seasons?.Count ?? 0,
        EpisodesCount = show.Seasons?.Sum(s => s.Episodes?.Count ?? 0) ?? 0,
        UserStatus = userShow?.Status.ToString(),
        UserRating = userShow?.UserRating,
        IsFavorite = userShow?.IsFavorite ?? false
    };
}
