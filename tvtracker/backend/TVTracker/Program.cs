using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Npgsql;
using TVTracker.Data;
using TVTracker.Models;
using TVTracker.Services;

var builder = WebApplication.CreateBuilder(args);

static string ResolveConnectionString(IConfiguration config)
{
    var dbUrl = Environment.GetEnvironmentVariable("DATABASE_URL")
             ?? config.GetConnectionString("DefaultConnection") ?? "";
    if (dbUrl.StartsWith("postgresql://") || dbUrl.StartsWith("postgres://"))
    {
        var uri = new Uri(dbUrl);
        var user = uri.UserInfo.Split(':')[0];
        var pass = uri.UserInfo.Contains(':') ? uri.UserInfo[(uri.UserInfo.IndexOf(':') + 1)..] : "";
        return $"Host={uri.Host};Port={(uri.Port > 0 ? uri.Port : 5432)};Database={uri.AbsolutePath.TrimStart('/')};Username={user};Password={pass};SSL Mode=Require;Trust Server Certificate=true";
    }
    return dbUrl;
}

var connStr = ResolveConnectionString(builder.Configuration);

builder.Services.AddDbContext<ApplicationDbContext>(o => o.UseNpgsql(connStr));
builder.Services.AddIdentityCore<AppUser>(opt =>
{
    opt.Password.RequireNonAlphanumeric = false;
    opt.Password.RequireDigit = true;
    opt.Password.RequiredLength = 6;
    opt.Password.RequireUppercase = false;
    opt.User.RequireUniqueEmail = true;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddSignInManager<SignInManager<AppUser>>();

var jwtKey = builder.Configuration["JwtSettings:SecretKey"] ?? "super-secret-key-at-least-32-chars!!";
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(o => o.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
        ValidateIssuer = true, ValidIssuer = builder.Configuration["JwtSettings:Issuer"] ?? "tvtracker",
        ValidateAudience = true, ValidAudience = builder.Configuration["JwtSettings:Audience"] ?? "tvtracker-users",
        ValidateLifetime = true, ClockSkew = TimeSpan.Zero
    });

builder.Services.AddAuthorization();
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddHttpClient();
builder.Services.AddControllers();

builder.Services.AddCors(o => o.AddPolicy("AllowReact", p =>
    p.SetIsOriginAllowed(_ => true).AllowAnyMethod().AllowAnyHeader().AllowCredentials()));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "TV Tracker API", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme { Description = "JWT Bearer", Name = "Authorization", In = ParameterLocation.Header, Type = SecuritySchemeType.ApiKey, Scheme = "Bearer" });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement { { new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" } }, Array.Empty<string>() } });
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db  = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var log = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    log.LogInformation("Initializing database...");

    await db.Database.EnsureCreatedAsync();
    log.LogInformation("Identity tables ready.");

    await using var conn = new NpgsqlConnection(connStr);
    await conn.OpenAsync();
    await using var cmd = conn.CreateCommand();
    cmd.CommandText = @"
CREATE TABLE IF NOT EXISTS ""Shows"" (""Id"" SERIAL PRIMARY KEY, ""Title"" TEXT NOT NULL, ""Description"" TEXT, ""PosterUrl"" TEXT, ""BackdropUrl"" TEXT, ""Genre"" TEXT, ""Network"" TEXT, ""Status"" TEXT NOT NULL DEFAULT 'Ongoing', ""TmdbId"" INTEGER, ""AverageRating"" NUMERIC(4,2) NOT NULL DEFAULT 0, ""RatingCount"" INTEGER NOT NULL DEFAULT 0, ""FirstAirDate"" TIMESTAMPTZ, ""LastAirDate"" TIMESTAMPTZ, ""CreatedAt"" TIMESTAMPTZ NOT NULL DEFAULT NOW());
CREATE UNIQUE INDEX IF NOT EXISTS ""IX_Shows_TmdbId"" ON ""Shows""(""TmdbId"") WHERE ""TmdbId"" IS NOT NULL;
CREATE TABLE IF NOT EXISTS ""Seasons"" (""Id"" SERIAL PRIMARY KEY, ""ShowId"" INTEGER NOT NULL REFERENCES ""Shows""(""Id"") ON DELETE CASCADE, ""SeasonNumber"" INTEGER NOT NULL, ""Title"" TEXT, ""Description"" TEXT, ""PosterUrl"" TEXT, ""AirDate"" TIMESTAMPTZ);
CREATE TABLE IF NOT EXISTS ""Episodes"" (""Id"" SERIAL PRIMARY KEY, ""SeasonId"" INTEGER NOT NULL REFERENCES ""Seasons""(""Id"") ON DELETE CASCADE, ""EpisodeNumber"" INTEGER NOT NULL, ""Title"" TEXT NOT NULL, ""Description"" TEXT, ""DurationMinutes"" INTEGER, ""AirDate"" TIMESTAMPTZ, ""ThumbnailUrl"" TEXT, ""AverageRating"" NUMERIC(4,2) NOT NULL DEFAULT 0, ""RatingCount"" INTEGER NOT NULL DEFAULT 0);
CREATE TABLE IF NOT EXISTS ""UserShows"" (""Id"" SERIAL PRIMARY KEY, ""UserId"" TEXT NOT NULL REFERENCES ""AspNetUsers""(""Id"") ON DELETE CASCADE, ""ShowId"" INTEGER NOT NULL REFERENCES ""Shows""(""Id"") ON DELETE CASCADE, ""Status"" TEXT NOT NULL DEFAULT 'PlanToWatch', ""UserRating"" INTEGER, ""IsFavorite"" BOOLEAN NOT NULL DEFAULT FALSE, ""AddedAt"" TIMESTAMPTZ NOT NULL DEFAULT NOW(), ""StartedAt"" TIMESTAMPTZ, ""FinishedAt"" TIMESTAMPTZ, ""Notes"" TEXT);
CREATE UNIQUE INDEX IF NOT EXISTS ""IX_UserShows_UserId_ShowId"" ON ""UserShows""(""UserId"", ""ShowId"");
CREATE TABLE IF NOT EXISTS ""WatchedEpisodes"" (""Id"" SERIAL PRIMARY KEY, ""UserId"" TEXT NOT NULL REFERENCES ""AspNetUsers""(""Id"") ON DELETE CASCADE, ""EpisodeId"" INTEGER NOT NULL REFERENCES ""Episodes""(""Id"") ON DELETE CASCADE, ""WatchedAt"" TIMESTAMPTZ NOT NULL DEFAULT NOW());
CREATE UNIQUE INDEX IF NOT EXISTS ""IX_WatchedEpisodes_UserId_EpisodeId"" ON ""WatchedEpisodes""(""UserId"", ""EpisodeId"");
CREATE TABLE IF NOT EXISTS ""EpisodeRatings"" (""Id"" SERIAL PRIMARY KEY, ""UserId"" TEXT NOT NULL REFERENCES ""AspNetUsers""(""Id"") ON DELETE CASCADE, ""EpisodeId"" INTEGER NOT NULL REFERENCES ""Episodes""(""Id"") ON DELETE CASCADE, ""Rating"" INTEGER NOT NULL, ""Comment"" TEXT, ""CreatedAt"" TIMESTAMPTZ NOT NULL DEFAULT NOW());
CREATE UNIQUE INDEX IF NOT EXISTS ""IX_EpisodeRatings_UserId_EpisodeId"" ON ""EpisodeRatings""(""UserId"", ""EpisodeId"");
CREATE TABLE IF NOT EXISTS ""ShowReviews"" (""Id"" SERIAL PRIMARY KEY, ""UserId"" TEXT NOT NULL REFERENCES ""AspNetUsers""(""Id"") ON DELETE CASCADE, ""ShowId"" INTEGER NOT NULL REFERENCES ""Shows""(""Id"") ON DELETE CASCADE, ""Content"" TEXT NOT NULL, ""Rating"" INTEGER NOT NULL, ""ContainsSpoilers"" BOOLEAN NOT NULL DEFAULT FALSE, ""CreatedAt"" TIMESTAMPTZ NOT NULL DEFAULT NOW(), ""UpdatedAt"" TIMESTAMPTZ);
CREATE UNIQUE INDEX IF NOT EXISTS ""IX_ShowReviews_UserId_ShowId"" ON ""ShowReviews""(""UserId"", ""ShowId"");
";
    await cmd.ExecuteNonQueryAsync();
    log.LogInformation("Custom tables ready.");

    try
    {
        if (!await db.Shows.AnyAsync())
        {
            log.LogInformation("Seeding shows...");
            db.Shows.AddRange(
                new Show { Title="Breaking Bad",        Description="A chemistry teacher turned drug lord.",                  Genre="Crime,Drama",          Network="AMC",      Status="Ended",   TmdbId=1396,   AverageRating=9.5, PosterUrl="https://image.tmdb.org/t/p/w500/ggFHVNu6YYI5L9pCfOacjizRGt.jpg",   BackdropUrl="https://image.tmdb.org/t/p/w1280/tsRy63Mu5cu8etL1X7ZLyf7UP1M.jpg", CreatedAt=DateTime.UtcNow, FirstAirDate=new DateTime(2008,1,20,0,0,0,DateTimeKind.Utc), LastAirDate=new DateTime(2013,9,29,0,0,0,DateTimeKind.Utc) },
                new Show { Title="Game of Thrones",     Description="Seven noble families fight for control of the mythical land of Westeros.",  Genre="Fantasy,Drama,Action",  Network="HBO",    Status="Ended",   TmdbId=1399,   AverageRating=8.2, PosterUrl="https://image.tmdb.org/t/p/w500/1XS1oqL89opfnbLl8WnZY1O1uJx.jpg",   BackdropUrl="https://image.tmdb.org/t/p/w1280/suopoADq0k8YZr4dQXcU6aLIWjQ.jpg", CreatedAt=DateTime.UtcNow, FirstAirDate=new DateTime(2011,4,17,0,0,0,DateTimeKind.Utc), LastAirDate=new DateTime(2019,5,19,0,0,0,DateTimeKind.Utc) },
                new Show { Title="Stranger Things",     Description="When a young boy vanishes, a small town uncovers a mystery of secret experiments and supernatural forces.", Genre="Sci-Fi,Horror,Drama", Network="Netflix", Status="Ended", TmdbId=66732, AverageRating=8.7, PosterUrl="https://image.tmdb.org/t/p/w500/49WJfeN0moxb9IPfGn8AIqMGskD.jpg", BackdropUrl="https://image.tmdb.org/t/p/w1280/56v2KjBlU4XaOv9rVYEQypROD7P.jpg", CreatedAt=DateTime.UtcNow, FirstAirDate=new DateTime(2016,7,15,0,0,0,DateTimeKind.Utc) },
                new Show { Title="The Crown",           Description="Follows the political rivalries and romance of Queen Elizabeth II's reign.",   Genre="Drama,History",       Network="Netflix", Status="Ended",   TmdbId=65494,  AverageRating=8.1, PosterUrl="https://image.tmdb.org/t/p/w500/1M876KPjulVwppEpldhdc8V4o68.jpg",  BackdropUrl="https://image.tmdb.org/t/p/w1280/xDrLtGCfuEJNwuFGqvgFfgDvYNT.jpg", CreatedAt=DateTime.UtcNow, FirstAirDate=new DateTime(2016,11,4,0,0,0,DateTimeKind.Utc) },
                new Show { Title="Succession",          Description="The Roy family controls one of the biggest media and entertainment conglomerates in the world.", Genre="Drama,Comedy", Network="HBO", Status="Ended", TmdbId=76669, AverageRating=8.8, PosterUrl="https://image.tmdb.org/t/p/w500/e2X8xSBOrKoMDFLRNPwQHckCm4a.jpg", BackdropUrl="https://image.tmdb.org/t/p/w1280/b7e6L0SRCCeqt6yIbOQEMB5K16G.jpg", CreatedAt=DateTime.UtcNow, FirstAirDate=new DateTime(2018,6,3,0,0,0,DateTimeKind.Utc) },
                new Show { Title="The Last of Us",      Description="In a post-apocalyptic world, a smuggler must transport a teenage girl across the country.",  Genre="Drama,Action,Sci-Fi", Network="HBO", Status="Ongoing", TmdbId=100088, AverageRating=8.9, PosterUrl="https://image.tmdb.org/t/p/w500/uKvVjHNqB5VmOrdxqAt2F7J78ED.jpg", BackdropUrl="https://image.tmdb.org/t/p/w1280/uDgy6hyPd82kOHh6I95iFRegion.jpg", CreatedAt=DateTime.UtcNow, FirstAirDate=new DateTime(2023,1,15,0,0,0,DateTimeKind.Utc) },
                new Show { Title="Wednesday",           Description="Wednesday Addams is a student at Nevermore Academy, solving a mystery while trying to master her psychic power.", Genre="Mystery,Horror,Comedy,Fantasy", Network="Netflix", Status="Ongoing", TmdbId=119051, AverageRating=8.1, PosterUrl="https://image.tmdb.org/t/p/w500/9PFonBhy4cQy7Jz20NpMygczOkv.jpg", BackdropUrl="https://image.tmdb.org/t/p/w1280/iHSwvRVsRyxpX7FE7GbviaBvmAx.jpg", CreatedAt=DateTime.UtcNow, FirstAirDate=new DateTime(2022,11,23,0,0,0,DateTimeKind.Utc) },
                new Show { Title="House of the Dragon", Description="The story of House Targaryen, set 200 years before the events of Game of Thrones.", Genre="Fantasy,Drama,Action", Network="HBO", Status="Ongoing", TmdbId=94997, AverageRating=8.4, PosterUrl="https://image.tmdb.org/t/p/w500/z2yahl2uefxDCl0nogcRBstwruJ.jpg", BackdropUrl="https://image.tmdb.org/t/p/w1280/etj8E2o0Bud0HkONVQPjyCkIvpv.jpg", CreatedAt=DateTime.UtcNow, FirstAirDate=new DateTime(2022,8,21,0,0,0,DateTimeKind.Utc) },
                new Show { Title="The Bear",            Description="A young chef from fine dining returns to Chicago to run his family's sandwich shop.", Genre="Drama,Comedy", Network="FX", Status="Ongoing", TmdbId=136315, AverageRating=8.6, PosterUrl="https://image.tmdb.org/t/p/w500/sHFlbKS3WLqMnp9t2ghADIJFnuQ.jpg", BackdropUrl="https://image.tmdb.org/t/p/w1280/rSbV2Bva6V8HZDQB2yqMXMBqVJX.jpg", CreatedAt=DateTime.UtcNow, FirstAirDate=new DateTime(2022,6,23,0,0,0,DateTimeKind.Utc) },
                new Show { Title="Severance",           Description="Mark leads a team of office workers whose memories have been surgically divided between their work and personal lives.", Genre="Sci-Fi,Thriller,Drama,Mystery", Network="Apple TV+", Status="Ongoing", TmdbId=95396, AverageRating=8.7, PosterUrl="https://image.tmdb.org/t/p/w500/lgkgS4QQmqxMQUGTBjbp44LzFM6.jpg", BackdropUrl="https://image.tmdb.org/t/p/w1280/3yJBXcRxIc5OHQGG5LPkUKGHYAS.jpg", CreatedAt=DateTime.UtcNow, FirstAirDate=new DateTime(2022,2,18,0,0,0,DateTimeKind.Utc) },
                new Show { Title="The White Lotus",     Description="A social satire following guests and workers at an exclusive Hawaiian resort.", Genre="Drama,Comedy,Mystery", Network="HBO", Status="Ongoing", TmdbId=120168, AverageRating=7.9, PosterUrl="https://image.tmdb.org/t/p/w500/aQPeznSu7XDTrrdCtT5eLiu52Yu.jpg", BackdropUrl="https://image.tmdb.org/t/p/w1280/kpSS1DWzBEkS9VJSbB4ORSJ3LNE.jpg", CreatedAt=DateTime.UtcNow, FirstAirDate=new DateTime(2021,7,11,0,0,0,DateTimeKind.Utc) },
                new Show { Title="Euphoria",            Description="A group of high school students navigate love and friendships in a world of drugs, sex and violence.", Genre="Drama", Network="HBO", Status="Ongoing", TmdbId=85552, AverageRating=8.3, PosterUrl="https://image.tmdb.org/t/p/w500/3Q0hd3heuWwDWpwcDkhQlCER9dD.jpg", BackdropUrl="https://image.tmdb.org/t/p/w1280/wqRVnMKsPlkDuFHQqrYiZ0ZQ9Sc.jpg", CreatedAt=DateTime.UtcNow, FirstAirDate=new DateTime(2019,6,16,0,0,0,DateTimeKind.Utc) },
                new Show { Title="Peaky Blinders",      Description="A gangster family epic set in 1919 Birmingham, England.", Genre="Crime,Drama,History", Network="BBC One", Status="Ended", TmdbId=60574, AverageRating=8.8, PosterUrl="https://image.tmdb.org/t/p/w500/vUUqzWa2LnHIVqkaKVlVGkPmL3Z.jpg", BackdropUrl="https://image.tmdb.org/t/p/w1280/wiE9doxiLwq3WCGamDIOb2PqBqc.jpg", CreatedAt=DateTime.UtcNow, FirstAirDate=new DateTime(2013,9,12,0,0,0,DateTimeKind.Utc), LastAirDate=new DateTime(2022,4,3,0,0,0,DateTimeKind.Utc) },
                new Show { Title="The Witcher",         Description="Geralt of Rivia, a solitary monster hunter, struggles to find his place in a world where people often prove more wicked than beasts.", Genre="Fantasy,Action,Adventure", Network="Netflix", Status="Ongoing", TmdbId=71912, AverageRating=8.0, PosterUrl="https://image.tmdb.org/t/p/w500/7vjaCdMw15FEbXyLQTVa04URsPm.jpg", BackdropUrl="https://image.tmdb.org/t/p/w1280/jBJWaqoSCiARWtfV0GlqHrcdidd.jpg", CreatedAt=DateTime.UtcNow, FirstAirDate=new DateTime(2019,12,20,0,0,0,DateTimeKind.Utc) },
                new Show { Title="Dark",                Description="A family saga with a supernatural twist, set in a German town where the disappearance of two young children exposes the links among four families.", Genre="Sci-Fi,Thriller,Mystery,Drama", Network="Netflix", Status="Ended", TmdbId=70523, AverageRating=8.7, PosterUrl="https://image.tmdb.org/t/p/w500/apbrbWs8M9lyOpJYU5WXrpFbk1Z.jpg", BackdropUrl="https://image.tmdb.org/t/p/w1280/nGF5T0BpFI5b0BogY1Nt5E1Jgjh.jpg", CreatedAt=DateTime.UtcNow, FirstAirDate=new DateTime(2017,12,1,0,0,0,DateTimeKind.Utc), LastAirDate=new DateTime(2020,6,27,0,0,0,DateTimeKind.Utc) },
                new Show { Title="Ozark",               Description="A financial advisor drags his family from Chicago to the Missouri Ozarks, where he must launder money.", Genre="Crime,Drama,Thriller", Network="Netflix", Status="Ended", TmdbId=69740, AverageRating=8.4, PosterUrl="https://image.tmdb.org/t/p/w500/pCGyPVrI9Fzc6rniTDBa3Oeqp7c.jpg", BackdropUrl="https://image.tmdb.org/t/p/w1280/mRbvHNpUJOIhSt1AktHQnkSMjEm.jpg", CreatedAt=DateTime.UtcNow, FirstAirDate=new DateTime(2017,7,21,0,0,0,DateTimeKind.Utc), LastAirDate=new DateTime(2022,4,29,0,0,0,DateTimeKind.Utc) },
                new Show { Title="Squid Game",          Description="Hundreds of cash-strapped players accept an invitation to compete in children's games. Inside, a deadly game awaits.", Genre="Thriller,Drama,Action", Network="Netflix", Status="Ongoing", TmdbId=93405, AverageRating=8.0, PosterUrl="https://image.tmdb.org/t/p/w500/dDlEmu3EZ0Pgg93K2SVNLCjCSvE.jpg", BackdropUrl="https://image.tmdb.org/t/p/w1280/qw3J9cNeLioOLoR68WX7z79aCdK.jpg", CreatedAt=DateTime.UtcNow, FirstAirDate=new DateTime(2021,9,17,0,0,0,DateTimeKind.Utc) },
                new Show { Title="Better Call Saul",    Description="The trials and tribulations of criminal lawyer Jimmy McGill in the time before his fateful encounter with Walter White.", Genre="Crime,Drama", Network="AMC", Status="Ended", TmdbId=60059, AverageRating=8.8, PosterUrl="https://image.tmdb.org/t/p/w500/fC2HDm5t0kHl7mTm7jxMR31b7by.jpg", BackdropUrl="https://image.tmdb.org/t/p/w1280/ggFHVNu6YYI5L9pCfOacjizRGt.jpg", CreatedAt=DateTime.UtcNow, FirstAirDate=new DateTime(2015,2,8,0,0,0,DateTimeKind.Utc), LastAirDate=new DateTime(2022,8,15,0,0,0,DateTimeKind.Utc) },
                new Show { Title="Andor",               Description="The fake rebel Cassian Andor's rogue path toward becoming a rebel hero.", Genre="Sci-Fi,Action,Drama", Network="Disney+", Status="Ongoing", TmdbId=83867, AverageRating=8.3, PosterUrl="https://image.tmdb.org/t/p/w500/59SVNwLfoMnZPPB6ukW6dlPxAdI.jpg", BackdropUrl="https://image.tmdb.org/t/p/w1280/r8Ph5MYXL04Qzu4QBbq2KjqwtkQ.jpg", CreatedAt=DateTime.UtcNow, FirstAirDate=new DateTime(2022,9,21,0,0,0,DateTimeKind.Utc) },
                new Show { Title="The Boys",            Description="A group of vigilantes set out to take down corrupt superheroes who abuse their powers.", Genre="Action,Sci-Fi,Comedy", Network="Amazon Prime", Status="Ended", TmdbId=76479, AverageRating=8.7, PosterUrl="https://image.tmdb.org/t/p/w500/stTEycfG9928HYGEISBFaG1ngjM.jpg", BackdropUrl="https://image.tmdb.org/t/p/w1280/mY7SeH4HFFxW1hiI6cWuwCRKptN.jpg", CreatedAt=DateTime.UtcNow, FirstAirDate=new DateTime(2019,7,26,0,0,0,DateTimeKind.Utc) }
            );
            await db.SaveChangesAsync();
            log.LogInformation("Seeded 20 shows with real posters.");
        }
    }
    catch (Exception ex) { log.LogError(ex, "Seeding failed: {Message}", ex.Message); }
}

app.UseSwagger();
app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "TV Tracker API v1"));
app.UseCors("AllowReact");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();
