-- This script runs automatically when PostgreSQL container starts fresh
-- It creates ALL tables needed by TVTracker

CREATE TABLE IF NOT EXISTS "AspNetRoles" (
    "Id" TEXT NOT NULL PRIMARY KEY,
    "Name" VARCHAR(256),
    "NormalizedName" VARCHAR(256),
    "ConcurrencyStamp" TEXT
);

CREATE TABLE IF NOT EXISTS "AspNetUsers" (
    "Id" TEXT NOT NULL PRIMARY KEY,
    "DisplayName" TEXT NOT NULL DEFAULT '',
    "AvatarUrl" TEXT,
    "CreatedAt" TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    "UserName" VARCHAR(256),
    "NormalizedUserName" VARCHAR(256),
    "Email" VARCHAR(256),
    "NormalizedEmail" VARCHAR(256),
    "EmailConfirmed" BOOLEAN NOT NULL DEFAULT FALSE,
    "PasswordHash" TEXT,
    "SecurityStamp" TEXT,
    "ConcurrencyStamp" TEXT,
    "PhoneNumber" TEXT,
    "PhoneNumberConfirmed" BOOLEAN NOT NULL DEFAULT FALSE,
    "TwoFactorEnabled" BOOLEAN NOT NULL DEFAULT FALSE,
    "LockoutEnd" TIMESTAMPTZ,
    "LockoutEnabled" BOOLEAN NOT NULL DEFAULT FALSE,
    "AccessFailedCount" INTEGER NOT NULL DEFAULT 0
);

CREATE TABLE IF NOT EXISTS "AspNetRoleClaims" (
    "Id" SERIAL PRIMARY KEY,
    "RoleId" TEXT NOT NULL REFERENCES "AspNetRoles"("Id") ON DELETE CASCADE,
    "ClaimType" TEXT,
    "ClaimValue" TEXT
);

CREATE TABLE IF NOT EXISTS "AspNetUserClaims" (
    "Id" SERIAL PRIMARY KEY,
    "UserId" TEXT NOT NULL REFERENCES "AspNetUsers"("Id") ON DELETE CASCADE,
    "ClaimType" TEXT,
    "ClaimValue" TEXT
);

CREATE TABLE IF NOT EXISTS "AspNetUserLogins" (
    "LoginProvider" TEXT NOT NULL,
    "ProviderKey" TEXT NOT NULL,
    "ProviderDisplayName" TEXT,
    "UserId" TEXT NOT NULL REFERENCES "AspNetUsers"("Id") ON DELETE CASCADE,
    PRIMARY KEY ("LoginProvider", "ProviderKey")
);

CREATE TABLE IF NOT EXISTS "AspNetUserRoles" (
    "UserId" TEXT NOT NULL REFERENCES "AspNetUsers"("Id") ON DELETE CASCADE,
    "RoleId" TEXT NOT NULL REFERENCES "AspNetRoles"("Id") ON DELETE CASCADE,
    PRIMARY KEY ("UserId", "RoleId")
);

CREATE TABLE IF NOT EXISTS "AspNetUserTokens" (
    "UserId" TEXT NOT NULL REFERENCES "AspNetUsers"("Id") ON DELETE CASCADE,
    "LoginProvider" TEXT NOT NULL,
    "Name" TEXT NOT NULL,
    "Value" TEXT,
    PRIMARY KEY ("UserId", "LoginProvider", "Name")
);

CREATE TABLE IF NOT EXISTS "Shows" (
    "Id" SERIAL PRIMARY KEY,
    "Title" TEXT NOT NULL,
    "Description" TEXT,
    "PosterUrl" TEXT,
    "BackdropUrl" TEXT,
    "Genre" TEXT,
    "Network" TEXT,
    "Status" TEXT NOT NULL DEFAULT 'Ongoing',
    "TmdbId" INTEGER,
    "AverageRating" NUMERIC(4,2) NOT NULL DEFAULT 0,
    "RatingCount" INTEGER NOT NULL DEFAULT 0,
    "FirstAirDate" TIMESTAMPTZ,
    "LastAirDate" TIMESTAMPTZ,
    "CreatedAt" TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE UNIQUE INDEX IF NOT EXISTS "IX_Shows_TmdbId"
    ON "Shows"("TmdbId") WHERE "TmdbId" IS NOT NULL;

CREATE TABLE IF NOT EXISTS "Seasons" (
    "Id" SERIAL PRIMARY KEY,
    "ShowId" INTEGER NOT NULL REFERENCES "Shows"("Id") ON DELETE CASCADE,
    "SeasonNumber" INTEGER NOT NULL,
    "Title" TEXT,
    "Description" TEXT,
    "PosterUrl" TEXT,
    "AirDate" TIMESTAMPTZ
);

CREATE INDEX IF NOT EXISTS "IX_Seasons_ShowId" ON "Seasons"("ShowId");

CREATE TABLE IF NOT EXISTS "Episodes" (
    "Id" SERIAL PRIMARY KEY,
    "SeasonId" INTEGER NOT NULL REFERENCES "Seasons"("Id") ON DELETE CASCADE,
    "EpisodeNumber" INTEGER NOT NULL,
    "Title" TEXT NOT NULL,
    "Description" TEXT,
    "DurationMinutes" INTEGER,
    "AirDate" TIMESTAMPTZ,
    "ThumbnailUrl" TEXT,
    "AverageRating" NUMERIC(4,2) NOT NULL DEFAULT 0,
    "RatingCount" INTEGER NOT NULL DEFAULT 0
);

CREATE INDEX IF NOT EXISTS "IX_Episodes_SeasonId" ON "Episodes"("SeasonId");

CREATE TABLE IF NOT EXISTS "UserShows" (
    "Id" SERIAL PRIMARY KEY,
    "UserId" TEXT NOT NULL REFERENCES "AspNetUsers"("Id") ON DELETE CASCADE,
    "ShowId" INTEGER NOT NULL REFERENCES "Shows"("Id") ON DELETE CASCADE,
    "Status" TEXT NOT NULL DEFAULT 'PlanToWatch',
    "UserRating" INTEGER,
    "IsFavorite" BOOLEAN NOT NULL DEFAULT FALSE,
    "AddedAt" TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    "StartedAt" TIMESTAMPTZ,
    "FinishedAt" TIMESTAMPTZ,
    "Notes" TEXT
);

CREATE UNIQUE INDEX IF NOT EXISTS "IX_UserShows_UserId_ShowId" ON "UserShows"("UserId", "ShowId");
CREATE INDEX IF NOT EXISTS "IX_UserShows_ShowId" ON "UserShows"("ShowId");

CREATE TABLE IF NOT EXISTS "WatchedEpisodes" (
    "Id" SERIAL PRIMARY KEY,
    "UserId" TEXT NOT NULL REFERENCES "AspNetUsers"("Id") ON DELETE CASCADE,
    "EpisodeId" INTEGER NOT NULL REFERENCES "Episodes"("Id") ON DELETE CASCADE,
    "WatchedAt" TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE UNIQUE INDEX IF NOT EXISTS "IX_WatchedEpisodes_UserId_EpisodeId" ON "WatchedEpisodes"("UserId", "EpisodeId");
CREATE INDEX IF NOT EXISTS "IX_WatchedEpisodes_EpisodeId" ON "WatchedEpisodes"("EpisodeId");

CREATE TABLE IF NOT EXISTS "EpisodeRatings" (
    "Id" SERIAL PRIMARY KEY,
    "UserId" TEXT NOT NULL REFERENCES "AspNetUsers"("Id") ON DELETE CASCADE,
    "EpisodeId" INTEGER NOT NULL REFERENCES "Episodes"("Id") ON DELETE CASCADE,
    "Rating" INTEGER NOT NULL,
    "Comment" TEXT,
    "CreatedAt" TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE UNIQUE INDEX IF NOT EXISTS "IX_EpisodeRatings_UserId_EpisodeId" ON "EpisodeRatings"("UserId", "EpisodeId");

CREATE TABLE IF NOT EXISTS "ShowReviews" (
    "Id" SERIAL PRIMARY KEY,
    "UserId" TEXT NOT NULL REFERENCES "AspNetUsers"("Id") ON DELETE CASCADE,
    "ShowId" INTEGER NOT NULL REFERENCES "Shows"("Id") ON DELETE CASCADE,
    "Content" TEXT NOT NULL,
    "Rating" INTEGER NOT NULL,
    "ContainsSpoilers" BOOLEAN NOT NULL DEFAULT FALSE,
    "CreatedAt" TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    "UpdatedAt" TIMESTAMPTZ
);

CREATE UNIQUE INDEX IF NOT EXISTS "IX_ShowReviews_UserId_ShowId" ON "ShowReviews"("UserId", "ShowId");
CREATE INDEX IF NOT EXISTS "IX_ShowReviews_ShowId" ON "ShowReviews"("ShowId");

CREATE INDEX IF NOT EXISTS "EmailIndex" ON "AspNetUsers"("NormalizedEmail");
CREATE UNIQUE INDEX IF NOT EXISTS "UserNameIndex" ON "AspNetUsers"("NormalizedUserName") WHERE "NormalizedUserName" IS NOT NULL;
CREATE UNIQUE INDEX IF NOT EXISTS "RoleNameIndex" ON "AspNetRoles"("NormalizedName") WHERE "NormalizedName" IS NOT NULL;
CREATE INDEX IF NOT EXISTS "IX_AspNetUserClaims_UserId" ON "AspNetUserClaims"("UserId");
CREATE INDEX IF NOT EXISTS "IX_AspNetUserLogins_UserId" ON "AspNetUserLogins"("UserId");
CREATE INDEX IF NOT EXISTS "IX_AspNetUserRoles_RoleId" ON "AspNetUserRoles"("RoleId");
CREATE INDEX IF NOT EXISTS "IX_AspNetRoleClaims_RoleId" ON "AspNetRoleClaims"("RoleId");

-- Mark as migrated so EF doesn't complain
CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
    "MigrationId" VARCHAR(150) NOT NULL PRIMARY KEY,
    "ProductVersion" VARCHAR(32) NOT NULL
);

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20240101000000_InitialCreate', '8.0.11')
ON CONFLICT DO NOTHING;
