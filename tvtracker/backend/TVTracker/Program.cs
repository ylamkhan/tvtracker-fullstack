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

// ── Convertit postgresql:// → format Npgsql automatiquement ─────────────────
static string ResolveConnectionString(IConfiguration config)
{
    var dbUrl = Environment.GetEnvironmentVariable("DATABASE_URL")
             ?? config.GetConnectionString("DefaultConnection")
             ?? "";

    if (dbUrl.StartsWith("postgresql://") || dbUrl.StartsWith("postgres://"))
    {
        var uri = new Uri(dbUrl);
        var user = uri.UserInfo.Split(':')[0];
        var pass = uri.UserInfo.Contains(':') ? uri.UserInfo[(uri.UserInfo.IndexOf(':') + 1)..] : "";
        var host = uri.Host;
        var port = uri.Port > 0 ? uri.Port : 5432;
        var db   = uri.AbsolutePath.TrimStart('/');
        return $"Host={host};Port={port};Database={db};Username={user};Password={pass};SSL Mode=Require;Trust Server Certificate=true";
    }
    return dbUrl;
}

var connStr = ResolveConnectionString(builder.Configuration);

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(connStr));

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

var jwtKey = builder.Configuration["JwtSettings:SecretKey"] ?? "super-secret-key-at-least-32-chars-long!!";
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ValidateIssuer = true,
            ValidIssuer = builder.Configuration["JwtSettings:Issuer"] ?? "tvtracker",
            ValidateAudience = true,
            ValidAudience = builder.Configuration["JwtSettings:Audience"] ?? "tvtracker-users",
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddControllers();

var frontendUrl = Environment.GetEnvironmentVariable("FRONTEND_URL") ?? "";
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReact", policy =>
    {
        var origins = new List<string> { "http://localhost:3000", "http://localhost:5173" };
        if (!string.IsNullOrEmpty(frontendUrl)) origins.Add(frontendUrl);
        // Allow all origins for Render testing
        policy.SetIsOriginAllowed(_ => true)
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "TV Tracker API", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization. Enter: Bearer {token}",
        Name = "Authorization", In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey, Scheme = "Bearer"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        { new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" } }, Array.Empty<string>() }
    });
});

var app = builder.Build();

// ── Init DB : crée toutes les tables (EF Identity + nos tables custom) ───────
using (var scope = app.Services.CreateScope())
{
    var db  = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var log = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    log.LogInformation("Initializing database...");

    // Étape 1 : EnsureCreated crée les tables Identity (AspNetUsers, etc.)
    await db.Database.EnsureCreatedAsync();
    log.LogInformation("Identity tables ready.");

    // Étape 2 : crée nos tables métier avec IF NOT EXISTS (safe à répéter)
    await using var conn = new NpgsqlConnection(connStr);
    await conn.OpenAsync();
    await using var cmd = conn.CreateCommand();
    cmd.CommandText = @"
CREATE TABLE IF NOT EXISTS ""Shows"" (""Id"" SERIAL PRIMARY KEY, ""Title"" TEXT NOT NULL, ""Description"" TEXT, ""PosterUrl"" TEXT, ""BackdropUrl"" TEXT, ""Genre"" TEXT, ""Network"" TEXT, ""Status"" TEXT NOT NULL DEFAULT 'Ongoing', ""TmdbId"" INTEGER, ""AverageRating"" NUMERIC(4,2) NOT NULL DEFAULT 0, ""RatingCount"" INTEGER NOT NULL DEFAULT 0, ""FirstAirDate"" TIMESTAMPTZ, ""LastAirDate"" TIMESTAMPTZ, ""CreatedAt"" TIMESTAMPTZ NOT NULL DEFAULT NOW());
CREATE UNIQUE INDEX IF NOT EXISTS ""IX_Shows_TmdbId"" ON ""Shows""(""TmdbId"") WHERE ""TmdbId"" IS NOT NULL;
CREATE TABLE IF NOT EXISTS ""Seasons"" (""Id"" SERIAL PRIMARY KEY, ""ShowId"" INTEGER NOT NULL REFERENCES ""Shows""(""Id"") ON DELETE CASCADE, ""SeasonNumber"" INTEGER NOT NULL, ""Title"" TEXT, ""Description"" TEXT, ""PosterUrl"" TEXT, ""AirDate"" TIMESTAMPTZ);
CREATE INDEX IF NOT EXISTS ""IX_Seasons_ShowId"" ON ""Seasons""(""ShowId"");
CREATE TABLE IF NOT EXISTS ""Episodes"" (""Id"" SERIAL PRIMARY KEY, ""SeasonId"" INTEGER NOT NULL REFERENCES ""Seasons""(""Id"") ON DELETE CASCADE, ""EpisodeNumber"" INTEGER NOT NULL, ""Title"" TEXT NOT NULL, ""Description"" TEXT, ""DurationMinutes"" INTEGER, ""AirDate"" TIMESTAMPTZ, ""ThumbnailUrl"" TEXT, ""AverageRating"" NUMERIC(4,2) NOT NULL DEFAULT 0, ""RatingCount"" INTEGER NOT NULL DEFAULT 0);
CREATE INDEX IF NOT EXISTS ""IX_Episodes_SeasonId"" ON ""Episodes""(""SeasonId"");
CREATE TABLE IF NOT EXISTS ""UserShows"" (""Id"" SERIAL PRIMARY KEY, ""UserId"" TEXT NOT NULL REFERENCES ""AspNetUsers""(""Id"") ON DELETE CASCADE, ""ShowId"" INTEGER NOT NULL REFERENCES ""Shows""(""Id"") ON DELETE CASCADE, ""Status"" TEXT NOT NULL DEFAULT 'PlanToWatch', ""UserRating"" INTEGER, ""IsFavorite"" BOOLEAN NOT NULL DEFAULT FALSE, ""AddedAt"" TIMESTAMPTZ NOT NULL DEFAULT NOW(), ""StartedAt"" TIMESTAMPTZ, ""FinishedAt"" TIMESTAMPTZ, ""Notes"" TEXT);
CREATE UNIQUE INDEX IF NOT EXISTS ""IX_UserShows_UserId_ShowId"" ON ""UserShows""(""UserId"", ""ShowId"");
CREATE INDEX IF NOT EXISTS ""IX_UserShows_ShowId"" ON ""UserShows""(""ShowId"");
CREATE TABLE IF NOT EXISTS ""WatchedEpisodes"" (""Id"" SERIAL PRIMARY KEY, ""UserId"" TEXT NOT NULL REFERENCES ""AspNetUsers""(""Id"") ON DELETE CASCADE, ""EpisodeId"" INTEGER NOT NULL REFERENCES ""Episodes""(""Id"") ON DELETE CASCADE, ""WatchedAt"" TIMESTAMPTZ NOT NULL DEFAULT NOW());
CREATE UNIQUE INDEX IF NOT EXISTS ""IX_WatchedEpisodes_UserId_EpisodeId"" ON ""WatchedEpisodes""(""UserId"", ""EpisodeId"");
CREATE INDEX IF NOT EXISTS ""IX_WatchedEpisodes_EpisodeId"" ON ""WatchedEpisodes""(""EpisodeId"");
CREATE TABLE IF NOT EXISTS ""EpisodeRatings"" (""Id"" SERIAL PRIMARY KEY, ""UserId"" TEXT NOT NULL REFERENCES ""AspNetUsers""(""Id"") ON DELETE CASCADE, ""EpisodeId"" INTEGER NOT NULL REFERENCES ""Episodes""(""Id"") ON DELETE CASCADE, ""Rating"" INTEGER NOT NULL, ""Comment"" TEXT, ""CreatedAt"" TIMESTAMPTZ NOT NULL DEFAULT NOW());
CREATE UNIQUE INDEX IF NOT EXISTS ""IX_EpisodeRatings_UserId_EpisodeId"" ON ""EpisodeRatings""(""UserId"", ""EpisodeId"");
CREATE TABLE IF NOT EXISTS ""ShowReviews"" (""Id"" SERIAL PRIMARY KEY, ""UserId"" TEXT NOT NULL REFERENCES ""AspNetUsers""(""Id"") ON DELETE CASCADE, ""ShowId"" INTEGER NOT NULL REFERENCES ""Shows""(""Id"") ON DELETE CASCADE, ""Content"" TEXT NOT NULL, ""Rating"" INTEGER NOT NULL, ""ContainsSpoilers"" BOOLEAN NOT NULL DEFAULT FALSE, ""CreatedAt"" TIMESTAMPTZ NOT NULL DEFAULT NOW(), ""UpdatedAt"" TIMESTAMPTZ);
CREATE UNIQUE INDEX IF NOT EXISTS ""IX_ShowReviews_UserId_ShowId"" ON ""ShowReviews""(""UserId"", ""ShowId"");
CREATE INDEX IF NOT EXISTS ""IX_ShowReviews_ShowId"" ON ""ShowReviews""(""ShowId"");
";
    await cmd.ExecuteNonQueryAsync();
    log.LogInformation("Custom tables ready.");

    // Étape 3 : Seed les séries si vide
    try
    {
        if (!await db.Shows.AnyAsync())
        {
            log.LogInformation("Seeding shows...");
            db.Shows.AddRange(
                new Show { Title="Breaking Bad",        Description="A chemistry teacher turned drug lord.",                  Genre="Crime,Drama",          Network="AMC",      Status="Ended",   TmdbId=1396,   AverageRating=9.5, CreatedAt=DateTime.UtcNow, FirstAirDate=new DateTime(2008,1,20,0,0,0,DateTimeKind.Utc), LastAirDate=new DateTime(2013,9,29,0,0,0,DateTimeKind.Utc) },
                new Show { Title="Game of Thrones",     Description="Noble families fight for the Iron Throne.",             Genre="Fantasy,Drama",         Network="HBO",      Status="Ended",   TmdbId=1399,   AverageRating=8.2, CreatedAt=DateTime.UtcNow, FirstAirDate=new DateTime(2011,4,17,0,0,0,DateTimeKind.Utc), LastAirDate=new DateTime(2019,5,19,0,0,0,DateTimeKind.Utc) },
                new Show { Title="Stranger Things",     Description="Kids face supernatural events in Hawkins.",             Genre="Sci-Fi,Horror,Drama",   Network="Netflix",  Status="Ended",   TmdbId=66732,  AverageRating=8.7, CreatedAt=DateTime.UtcNow, FirstAirDate=new DateTime(2016,7,15,0,0,0,DateTimeKind.Utc) },
                new Show { Title="The Crown",           Description="The reign of Queen Elizabeth II.",                      Genre="Drama,History",         Network="Netflix",  Status="Ended",   TmdbId=65494,  AverageRating=8.1, CreatedAt=DateTime.UtcNow, FirstAirDate=new DateTime(2016,11,4,0,0,0,DateTimeKind.Utc) },
                new Show { Title="Succession",          Description="A media dynasty fights for power.",                     Genre="Drama",                 Network="HBO",      Status="Ended",   TmdbId=76669,  AverageRating=8.8, CreatedAt=DateTime.UtcNow, FirstAirDate=new DateTime(2018,6,3,0,0,0,DateTimeKind.Utc) },
                new Show { Title="The Last of Us",      Description="A smuggler escorts a girl across post-apocalyptic US.", Genre="Drama,Action",          Network="HBO",      Status="Ongoing", TmdbId=100088, AverageRating=8.9, CreatedAt=DateTime.UtcNow, FirstAirDate=new DateTime(2023,1,15,0,0,0,DateTimeKind.Utc) },
                new Show { Title="Wednesday",           Description="Wednesday Addams at Nevermore Academy.",               Genre="Mystery,Horror,Comedy", Network="Netflix",  Status="Ongoing", TmdbId=119051, AverageRating=8.1, CreatedAt=DateTime.UtcNow, FirstAirDate=new DateTime(2022,11,23,0,0,0,DateTimeKind.Utc) },
                new Show { Title="House of the Dragon", Description="Prequel to Game of Thrones.",                          Genre="Fantasy,Drama",         Network="HBO",      Status="Ongoing", TmdbId=94997,  AverageRating=8.4, CreatedAt=DateTime.UtcNow, FirstAirDate=new DateTime(2022,8,21,0,0,0,DateTimeKind.Utc) },
                new Show { Title="The Bear",            Description="A chef returns home to run the family sandwich shop.",  Genre="Drama,Comedy",          Network="FX",       Status="Ongoing", TmdbId=136315, AverageRating=8.6, CreatedAt=DateTime.UtcNow, FirstAirDate=new DateTime(2022,6,23,0,0,0,DateTimeKind.Utc) },
                new Show { Title="Severance",           Description="Employees work and personal memories are separated.",  Genre="Sci-Fi,Thriller",       Network="Apple TV+",Status="Ongoing", TmdbId=95396,  AverageRating=8.7, CreatedAt=DateTime.UtcNow, FirstAirDate=new DateTime(2022,2,18,0,0,0,DateTimeKind.Utc) }
            );
            await db.SaveChangesAsync();
            log.LogInformation("Seeded 10 shows.");
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
