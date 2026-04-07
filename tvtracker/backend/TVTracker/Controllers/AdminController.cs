using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TVTracker.Data;
using TVTracker.Models;

namespace TVTracker.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AdminController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly IHttpClientFactory _http;

    public AdminController(ApplicationDbContext db, IHttpClientFactory http)
    {
        _db = db;
        _http = http;
    }

    /// <summary>
    /// Importe les séries TV populaires depuis TMDB
    /// Usage: POST /api/admin/import-tmdb?apiKey=VOTRE_CLE&pages=5
    /// </summary>
    [HttpPost("import-tmdb")]
    public async Task<IActionResult> ImportFromTmdb([FromQuery] string apiKey, [FromQuery] int pages = 3)
    {
        if (string.IsNullOrEmpty(apiKey))
            return BadRequest(new { message = "Fournir ?apiKey=VOTRE_CLE_TMDB — clé gratuite sur themoviedb.org" });

        var client = _http.CreateClient();
        var imported = 0;
        var skipped = 0;
        var errors = new List<string>();

        for (int page = 1; page <= Math.Min(pages, 10); page++)
        {
            try
            {
                var url = $"https://api.themoviedb.org/3/tv/popular?api_key={apiKey}&language=en-US&page={page}";
                var response = await client.GetStringAsync(url);
                var json = JsonDocument.Parse(response);
                var results = json.RootElement.GetProperty("results").EnumerateArray().ToList();

                foreach (var item in results)
                {
                    try
                    {
                        var tmdbId = item.GetProperty("id").GetInt32();
                        if (await _db.Shows.AnyAsync(s => s.TmdbId == tmdbId)) { skipped++; continue; }

                        var detailUrl = $"https://api.themoviedb.org/3/tv/{tmdbId}?api_key={apiKey}&language=en-US";
                        var detailStr = await client.GetStringAsync(detailUrl);
                        var d = JsonDocument.Parse(detailStr).RootElement;

                        string? GetStr(string key) => d.TryGetProperty(key, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() : null;
                        double GetDbl(string key) => d.TryGetProperty(key, out var v) && v.ValueKind == JsonValueKind.Number ? v.GetDouble() : 0;
                        int GetInt(string key) => d.TryGetProperty(key, out var v) && v.ValueKind == JsonValueKind.Number ? v.GetInt32() : 0;

                        var poster   = GetStr("poster_path");
                        var backdrop = GetStr("backdrop_path");
                        var rawStatus = GetStr("status") ?? "";

                        var genres = new List<string>();
                        if (d.TryGetProperty("genres", out var ga))
                            foreach (var g in ga.EnumerateArray())
                                if (g.TryGetProperty("name", out var gn) && gn.GetString() is { } s) genres.Add(s);

                        var network = "";
                        if (d.TryGetProperty("networks", out var na) && na.GetArrayLength() > 0)
                            if (na[0].TryGetProperty("name", out var nn)) network = nn.GetString() ?? "";

                        DateTime? ParseDate(string key)
                        {
                            var s = GetStr(key);
                            if (string.IsNullOrEmpty(s)) return null;
                            return DateTime.TryParse(s, out var dt) ? DateTime.SpecifyKind(dt, DateTimeKind.Utc) : null;
                        }

                        var mappedStatus = rawStatus switch
                        {
                            "Returning Series" or "In Production" or "Planned" => "Ongoing",
                            "Ended" or "Canceled" or "Cancelled" => "Ended",
                            _ => "Ongoing"
                        };

                        _db.Shows.Add(new Show
                        {
                            Title       = GetStr("name") ?? "Unknown",
                            Description = GetStr("overview"),
                            PosterUrl   = poster   != null ? $"https://image.tmdb.org/t/p/w500{poster}"   : null,
                            BackdropUrl = backdrop != null ? $"https://image.tmdb.org/t/p/w1280{backdrop}" : null,
                            Genre       = genres.Count > 0 ? string.Join(",", genres) : null,
                            Network     = network,
                            Status      = mappedStatus,
                            TmdbId      = tmdbId,
                            AverageRating = Math.Round(GetDbl("vote_average"), 2),
                            RatingCount   = GetInt("vote_count"),
                            FirstAirDate  = ParseDate("first_air_date"),
                            LastAirDate   = ParseDate("last_air_date"),
                            CreatedAt     = DateTime.UtcNow
                        });
                        imported++;
                        await Task.Delay(80);
                    }
                    catch (Exception ex) { errors.Add(ex.Message); }
                }

                await _db.SaveChangesAsync();
                await Task.Delay(300);
            }
            catch (Exception ex) { errors.Add($"Page {page}: {ex.Message}"); }
        }

        return Ok(new { message = "Import terminé!", imported, skipped, totalErrors = errors.Count, errors = errors.Take(5) });
    }

    /// <summary>Stats de la base de données</summary>
    [HttpGet("stats")]
    public async Task<IActionResult> GetStats() => Ok(new
    {
        shows    = await _db.Shows.CountAsync(),
        users    = await _db.Users.CountAsync(),
        episodes = await _db.Episodes.CountAsync(),
        reviews  = await _db.ShowReviews.CountAsync(),
    });

    /// <summary>Supprime toutes les séries (dangereux!)</summary>
    [HttpDelete("reset-shows")]
    public async Task<IActionResult> ResetShows([FromQuery] string confirm)
    {
        if (confirm != "yes") return BadRequest(new { message = "Ajouter ?confirm=yes pour confirmer" });
        await _db.Database.ExecuteSqlRawAsync(@"TRUNCATE ""Shows"" RESTART IDENTITY CASCADE");
        return Ok(new { message = "Toutes les séries supprimées." });
    }
}
