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

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

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

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReact", policy =>
        policy.WithOrigins("http://localhost:3000","http://localhost:5173","http://frontend")
              .AllowAnyMethod().AllowAnyHeader().AllowCredentials());
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

// ── Create tables directly via Npgsql (bypasses EF completely) ──────────────
var connStr = builder.Configuration.GetConnectionString("DefaultConnection")!;
await CreateTablesAsync(connStr);

// ── Seed shows via EF ────────────────────────────────────────────────────────
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var log = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
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
    catch (Exception ex) { log.LogError(ex, "Seeding failed."); throw; }
}

app.UseSwagger();
app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "TV Tracker API v1"));
app.UseCors("AllowReact");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();

// ── Pure Npgsql — no EF, no migrations, creates all tables if not exist ──────
static async Task CreateTablesAsync(string connectionString)
{
    await using var conn = new NpgsqlConnection(connectionString);
    await conn.OpenAsync();
    await using var cmd = conn.CreateCommand();
    cmd.CommandText = @"
CREATE TABLE IF NOT EXISTS ""AspNetRoles"" (""Id"" TEXT NOT NULL PRIMARY KEY, ""Name"" VARCHAR(256), ""NormalizedName"" VARCHAR(256), ""ConcurrencyStamp"" TEXT);
CREATE TABLE IF NOT EXISTS ""AspNetUsers"" (""Id"" TEXT NOT NULL PRIMARY KEY, ""DisplayName"" TEXT NOT NULL DEFAULT '', ""AvatarUrl"" TEXT, ""CreatedAt"" TIMESTAMPTZ NOT NULL DEFAULT NOW(), ""UserName"" VARCHAR(256), ""NormalizedUserName"" VARCHAR(256), ""Email"" VARCHAR(256), ""NormalizedEmail"" VARCHAR(256), ""EmailConfirmed"" BOOLEAN NOT NULL DEFAULT FALSE, ""PasswordHash"" TEXT, ""SecurityStamp"" TEXT, ""ConcurrencyStamp"" TEXT, ""PhoneNumber"" TEXT, ""PhoneNumberConfirmed"" BOOLEAN NOT NULL DEFAULT FALSE, ""TwoFactorEnabled"" BOOLEAN NOT NULL DEFAULT FALSE, ""LockoutEnd"" TIMESTAMPTZ, ""LockoutEnabled"" BOOLEAN NOT NULL DEFAULT FALSE, ""AccessFailedCount"" INTEGER NOT NULL DEFAULT 0);
CREATE TABLE IF NOT EXISTS ""AspNetRoleClaims"" (""Id"" SERIAL PRIMARY KEY, ""RoleId"" TEXT NOT NULL REFERENCES ""AspNetRoles""(""Id"") ON DELETE CASCADE, ""ClaimType"" TEXT, ""ClaimValue"" TEXT);
CREATE TABLE IF NOT EXISTS ""AspNetUserClaims"" (""Id"" SERIAL PRIMARY KEY, ""UserId"" TEXT NOT NULL REFERENCES ""AspNetUsers""(""Id"") ON DELETE CASCADE, ""ClaimType"" TEXT, ""ClaimValue"" TEXT);
CREATE TABLE IF NOT EXISTS ""AspNetUserLogins"" (""LoginProvider"" TEXT NOT NULL, ""ProviderKey"" TEXT NOT NULL, ""ProviderDisplayName"" TEXT, ""UserId"" TEXT NOT NULL REFERENCES ""AspNetUsers""(""Id"") ON DELETE CASCADE, PRIMARY KEY (""LoginProvider"", ""ProviderKey""));
CREATE TABLE IF NOT EXISTS ""AspNetUserRoles"" (""UserId"" TEXT NOT NULL REFERENCES ""AspNetUsers""(""Id"") ON DELETE CASCADE, ""RoleId"" TEXT NOT NULL REFERENCES ""AspNetRoles""(""Id"") ON DELETE CASCADE, PRIMARY KEY (""UserId"", ""RoleId""));
CREATE TABLE IF NOT EXISTS ""AspNetUserTokens"" (""UserId"" TEXT NOT NULL REFERENCES ""AspNetUsers""(""Id"") ON DELETE CASCADE, ""LoginProvider"" TEXT NOT NULL, ""Name"" TEXT NOT NULL, ""Value"" TEXT, PRIMARY KEY (""UserId"", ""LoginProvider"", ""Name""));
CREATE TABLE IF NOT EXISTS ""Shows"" (""Id"" SERIAL PRIMARY KEY, ""Title"" TEXT NOT NULL, ""Description"" TEXT, ""PosterUrl"" TEXT, ""BackdropUrl"" TEXT, ""Genre"" TEXT, ""Network"" TEXT, ""Status"" TEXT NOT NULL DEFAULT 'Ongoing', ""TmdbId"" INTEGER, ""AverageRating"" NUMERIC(4,2) NOT NULL DEFAULT 0, ""RatingCount"" INTEGER NOT NULL DEFAULT 0, ""FirstAirDate"" TIMESTAMPTZ, ""LastAirDate"" TIMESTAMPTZ, ""CreatedAt"" TIMESTAMPTZ NOT NULL DEFAULT NOW());
CREATE TABLE IF NOT EXISTS ""Seasons"" (""Id"" SERIAL PRIMARY KEY, ""ShowId"" INTEGER NOT NULL REFERENCES ""Shows""(""Id"") ON DELETE CASCADE, ""SeasonNumber"" INTEGER NOT NULL, ""Title"" TEXT, ""Description"" TEXT, ""PosterUrl"" TEXT, ""AirDate"" TIMESTAMPTZ);
CREATE TABLE IF NOT EXISTS ""Episodes"" (""Id"" SERIAL PRIMARY KEY, ""SeasonId"" INTEGER NOT NULL REFERENCES ""Seasons""(""Id"") ON DELETE CASCADE, ""EpisodeNumber"" INTEGER NOT NULL, ""Title"" TEXT NOT NULL, ""Description"" TEXT, ""DurationMinutes"" INTEGER, ""AirDate"" TIMESTAMPTZ, ""ThumbnailUrl"" TEXT, ""AverageRating"" NUMERIC(4,2) NOT NULL DEFAULT 0, ""RatingCount"" INTEGER NOT NULL DEFAULT 0);
CREATE TABLE IF NOT EXISTS ""UserShows"" (""Id"" SERIAL PRIMARY KEY, ""UserId"" TEXT NOT NULL REFERENCES ""AspNetUsers""(""Id"") ON DELETE CASCADE, ""ShowId"" INTEGER NOT NULL REFERENCES ""Shows""(""Id"") ON DELETE CASCADE, ""Status"" TEXT NOT NULL DEFAULT 'PlanToWatch', ""UserRating"" INTEGER, ""IsFavorite"" BOOLEAN NOT NULL DEFAULT FALSE, ""AddedAt"" TIMESTAMPTZ NOT NULL DEFAULT NOW(), ""StartedAt"" TIMESTAMPTZ, ""FinishedAt"" TIMESTAMPTZ, ""Notes"" TEXT);
CREATE TABLE IF NOT EXISTS ""WatchedEpisodes"" (""Id"" SERIAL PRIMARY KEY, ""UserId"" TEXT NOT NULL REFERENCES ""AspNetUsers""(""Id"") ON DELETE CASCADE, ""EpisodeId"" INTEGER NOT NULL REFERENCES ""Episodes""(""Id"") ON DELETE CASCADE, ""WatchedAt"" TIMESTAMPTZ NOT NULL DEFAULT NOW());
CREATE TABLE IF NOT EXISTS ""EpisodeRatings"" (""Id"" SERIAL PRIMARY KEY, ""UserId"" TEXT NOT NULL REFERENCES ""AspNetUsers""(""Id"") ON DELETE CASCADE, ""EpisodeId"" INTEGER NOT NULL REFERENCES ""Episodes""(""Id"") ON DELETE CASCADE, ""Rating"" INTEGER NOT NULL, ""Comment"" TEXT, ""CreatedAt"" TIMESTAMPTZ NOT NULL DEFAULT NOW());
CREATE TABLE IF NOT EXISTS ""ShowReviews"" (""Id"" SERIAL PRIMARY KEY, ""UserId"" TEXT NOT NULL REFERENCES ""AspNetUsers""(""Id"") ON DELETE CASCADE, ""ShowId"" INTEGER NOT NULL REFERENCES ""Shows""(""Id"") ON DELETE CASCADE, ""Content"" TEXT NOT NULL, ""Rating"" INTEGER NOT NULL, ""ContainsSpoilers"" BOOLEAN NOT NULL DEFAULT FALSE, ""CreatedAt"" TIMESTAMPTZ NOT NULL DEFAULT NOW(), ""UpdatedAt"" TIMESTAMPTZ);
CREATE UNIQUE INDEX IF NOT EXISTS ""IX_Shows_TmdbId"" ON ""Shows""(""TmdbId"") WHERE ""TmdbId"" IS NOT NULL;
CREATE UNIQUE INDEX IF NOT EXISTS ""IX_UserShows_UserId_ShowId"" ON ""UserShows""(""UserId"", ""ShowId"");
CREATE UNIQUE INDEX IF NOT EXISTS ""IX_WatchedEpisodes_UserId_EpisodeId"" ON ""WatchedEpisodes""(""UserId"", ""EpisodeId"");
CREATE UNIQUE INDEX IF NOT EXISTS ""IX_EpisodeRatings_UserId_EpisodeId"" ON ""EpisodeRatings""(""UserId"", ""EpisodeId"");
CREATE UNIQUE INDEX IF NOT EXISTS ""IX_ShowReviews_UserId_ShowId"" ON ""ShowReviews""(""UserId"", ""ShowId"");
CREATE INDEX IF NOT EXISTS ""EmailIndex"" ON ""AspNetUsers""(""NormalizedEmail"");
CREATE UNIQUE INDEX IF NOT EXISTS ""UserNameIndex"" ON ""AspNetUsers""(""NormalizedUserName"") WHERE ""NormalizedUserName"" IS NOT NULL;
CREATE UNIQUE INDEX IF NOT EXISTS ""RoleNameIndex"" ON ""AspNetRoles""(""NormalizedName"") WHERE ""NormalizedName"" IS NOT NULL;
";
    await cmd.ExecuteNonQueryAsync();
}