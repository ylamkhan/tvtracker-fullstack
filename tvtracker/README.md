# 📺 TVTracker — Full-Stack TV Series Tracking App

A complete, production-ready TV series tracker built with **ASP.NET Core 8**, **React 18**, and **PostgreSQL**.

---

## 🗂️ Project Structure

```
tvtracker/
├── docker-compose.yml              ← Run everything with one command
├── backend/
│   └── TVTracker/
│       ├── TVTracker.csproj        ← .NET 8 project + NuGet packages
│       ├── Program.cs              ← App bootstrap, DI, middleware
│       ├── appsettings.json        ← Configuration (DB, JWT)
│       ├── Dockerfile
│       ├── Models/
│       │   └── Models.cs           ← AppUser, Show, Season, Episode, UserShow, etc.
│       ├── Data/
│       │   └── ApplicationDbContext.cs  ← EF Core DbContext + seed data
│       ├── DTOs/
│       │   └── DTOs.cs             ← All request/response shapes
│       ├── Services/
│       │   └── TokenService.cs     ← JWT generation
│       ├── Controllers/
│       │   ├── AuthController.cs         ← POST /api/auth/register|login
│       │   ├── ShowsController.cs        ← CRUD shows, track, reviews
│       │   └── EpisodesAndUserController.cs ← Watch/rate episodes, user stats
│       └── Migrations/
│           └── 20240101000000_InitialCreate.cs
└── frontend/
    ├── package.json
    ├── vite.config.ts
    ├── index.html
    ├── Dockerfile
    ├── nginx.conf
    └── src/
        ├── main.tsx
        ├── App.tsx                  ← Router setup
        ├── index.css                ← Design system (CSS variables, utilities)
        ├── types/index.ts           ← TypeScript interfaces
        ├── services/api.ts          ← Axios API client
        ├── store/authStore.ts       ← Zustand auth state
        ├── components/
        │   ├── Layout.tsx           ← Navbar + footer
        │   ├── ShowCard.tsx         ← Reusable show card with progress
        │   └── StarRating.tsx       ← Interactive star rating
        └── pages/
            ├── HomePage.tsx         ← Hero + featured shows
            ├── BrowsePage.tsx       ← Search, filter, paginate
            ├── ShowDetailPage.tsx   ← Full show + episodes + reviews
            ├── MyListPage.tsx       ← User's tracked shows by status
            ├── ProfilePage.tsx      ← Stats dashboard + favorites
            ├── LoginPage.tsx
            └── RegisterPage.tsx
```

---

## ✨ Features

### User Authentication
- JWT-based registration & login (ASP.NET Identity)
- Persistent sessions via localStorage + Zustand
- Protected routes in React

### Show Tracking
- Browse & search all shows with filters (genre, status, pagination)
- Add shows to personal list with 5 statuses:
  - **Watching** / **Completed** / **On Hold** / **Dropped** / **Plan to Watch**
- Mark shows as favorites ❤️
- Rate shows (1–10 stars)
- Personal notes per show

### Episode Management
- Full season/episode accordion on show detail page
- Mark individual episodes as watched ✓
- Rate episodes (1–10) per episode
- Progress bar showing watched/total episodes

### Reviews & Ratings
- Write show reviews with spoiler warnings
- Community average rating on each show
- Per-episode average ratings

### Profile & Stats Dashboard
- Total shows tracked
- Episodes watched + time spent (hours/days)
- Average episode rating
- Favorites showcase
- Recently added shows

---

## 🚀 Quick Start — Docker (Recommended)

### Prerequisites
- [Docker Desktop](https://www.docker.com/products/docker-desktop/) installed

### Steps

```bash
# 1. Clone / unzip the project
cd tvtracker

# 2. Launch all services (DB + API + Frontend)
docker compose up --build

# 3. Open in browser
open http://localhost:3000
```

That's it! Docker will:
1. Start PostgreSQL and create the `tvtracker` database
2. Build and run the ASP.NET Core API on port 5000
3. Build and serve the React frontend on port 3000
4. Apply all EF Core migrations automatically on first start
5. Seed 8 popular TV shows

---

## 🛠️ Manual Development Setup

### Prerequisites
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Node.js 20+](https://nodejs.org/)
- [PostgreSQL 15+](https://www.postgresql.org/download/)

---

### 1. Database Setup

```sql
-- In psql or pgAdmin:
CREATE DATABASE tvtracker;
CREATE USER tvtracker_user WITH PASSWORD 'yourpassword';
GRANT ALL PRIVILEGES ON DATABASE tvtracker TO tvtracker_user;
```

---

### 2. Backend Setup

```bash
cd backend/TVTracker

# Update connection string in appsettings.json:
# "DefaultConnection": "Host=localhost;Port=5432;Database=tvtracker;Username=postgres;Password=yourpassword"

# Restore packages
dotnet restore

# Apply migrations (creates all tables + seeds data)
dotnet ef database update

# Run API (available at http://localhost:5000)
dotnet run
```

**Swagger UI** → http://localhost:5000/swagger

---

### 3. Frontend Setup

```bash
cd frontend

# Install dependencies
npm install

# Start dev server (available at http://localhost:3000)
npm run dev
```

---

## 🔌 API Reference

### Authentication
| Method | Endpoint | Body | Description |
|--------|----------|------|-------------|
| POST | `/api/auth/register` | `{email, password, displayName}` | Register new user |
| POST | `/api/auth/login` | `{email, password}` | Login, get JWT token |

### Shows
| Method | Endpoint | Auth | Description |
|--------|----------|------|-------------|
| GET | `/api/shows` | No | List shows (search, genre, status, page) |
| GET | `/api/shows/{id}` | No | Show detail with seasons, episodes, reviews |
| POST | `/api/shows` | ✅ | Create a new show |
| POST | `/api/shows/{id}/track` | ✅ | Add/update show in user list |
| DELETE | `/api/shows/{id}/track` | ✅ | Remove from user list |
| GET | `/api/shows/{id}/reviews` | No | Get all reviews |
| POST | `/api/shows/{id}/reviews` | ✅ | Write a review |

### Episodes
| Method | Endpoint | Auth | Description |
|--------|----------|------|-------------|
| POST | `/api/episodes/{id}/watch` | ✅ | Mark episode watched |
| DELETE | `/api/episodes/{id}/watch` | ✅ | Unmark episode watched |
| POST | `/api/episodes/{id}/rate` | ✅ | Rate an episode (1–10) |

### User
| Method | Endpoint | Auth | Description |
|--------|----------|------|-------------|
| GET | `/api/user/list` | ✅ | Get my tracked shows (filter by status) |
| GET | `/api/user/stats` | ✅ | Get watching stats & dashboard data |

---

## 🗄️ Database Schema

```
AspNetUsers (Identity)     Shows
├── Id (PK)                ├── Id (PK)
├── DisplayName            ├── Title
├── Email                  ├── Description
└── AvatarUrl              ├── PosterUrl / BackdropUrl
                           ├── Genre / Network / Status
UserShows                  ├── TmdbId
├── UserId (FK)            ├── AverageRating
├── ShowId (FK)            └── FirstAirDate / LastAirDate
├── Status (enum→text)
├── UserRating             Seasons
├── IsFavorite             ├── Id / ShowId (FK)
└── Notes                  └── SeasonNumber

WatchedEpisodes            Episodes
├── UserId (FK)            ├── Id / SeasonId (FK)
└── EpisodeId (FK)         ├── EpisodeNumber / Title
                           └── DurationMinutes / AirDate
EpisodeRatings
├── UserId (FK)            ShowReviews
├── EpisodeId (FK)         ├── UserId (FK) / ShowId (FK)
└── Rating (1-10)          ├── Content / Rating
                           └── ContainsSpoilers
```

---

## ⚙️ Configuration

### Backend — `appsettings.json`
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=tvtracker;Username=postgres;Password=YOUR_PASSWORD"
  },
  "JwtSettings": {
    "SecretKey": "your-super-secret-key-at-least-32-chars",
    "Issuer": "tvtracker",
    "Audience": "tvtracker-users"
  }
}
```

### Frontend — Vite Proxy (dev)
The `vite.config.ts` proxies `/api/*` → `http://localhost:5000`.
In production (Docker), nginx handles the proxy.

---

## 📦 Tech Stack

### Backend
| Package | Purpose |
|---------|---------|
| `ASP.NET Core 8` | Web API framework |
| `Entity Framework Core 8` | ORM + migrations |
| `Npgsql.EF.PostgreSQL` | PostgreSQL driver |
| `ASP.NET Identity` | User auth + password hashing |
| `JwtBearer` | JWT authentication |
| `Swashbuckle` | Swagger/OpenAPI docs |

### Frontend
| Package | Purpose |
|---------|---------|
| `React 18` | UI framework |
| `React Router 6` | Client-side routing |
| `@tanstack/react-query` | Server state, caching |
| `Zustand` | Client auth state |
| `Axios` | HTTP client |
| `react-hot-toast` | Notifications |
| `lucide-react` | Icons |
| `Vite` | Build tool |

---

## 🎨 Design System

Dark cinematic theme with CSS variables:
- **Font Display**: Bebas Neue (headings)
- **Font Body**: DM Sans
- **Accent**: `#7c3aed` (purple with glow effects)
- **Background**: `#0d0d1a` deep navy

---

## 🔒 Security Notes

For production deployment:
1. **Change the JWT secret key** in `appsettings.json` to a long random string
2. **Change the PostgreSQL password** in `docker-compose.yml`
3. Set `ASPNETCORE_ENVIRONMENT=Production`
4. Use HTTPS (add SSL certificate to nginx)
5. Consider adding rate limiting middleware

---

## 📈 Extending the App

Ideas to add next:
- **TMDB API integration** — auto-import shows with posters via [themoviedb.org](https://www.themoviedb.org/documentation/api)
- **Social features** — follow users, see what friends watch
- **Push notifications** — new episode alerts
- **Admin panel** — manage shows/episodes via UI
- **OAuth** — Login with Google/GitHub
- **Lists** — Custom watchlists beyond the 5 statuses

---

## 📄 License

MIT License — free to use and modify.
