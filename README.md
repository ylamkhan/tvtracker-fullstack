# TVTracker

A complete, production-ready TV series tracker built with **ASP.NET Core 8**, **React 18**, and **PostgreSQL**.

---

## Project Structure

```
tvtracker/
├── docker-compose.yml
├── backend/
│   └── TVTracker/
│       ├── TVTracker.csproj
│       ├── Program.cs
│       ├── appsettings.json
│       ├── Dockerfile
│       ├── Models/
│       │   └── Models.cs
│       ├── Data/
│       │   └── ApplicationDbContext.cs
│       ├── DTOs/
│       │   └── DTOs.cs
│       ├── Services/
│       │   └── TokenService.cs
│       ├── Controllers/
│       │   ├── AdminController.cs 
│       │   ├── AuthController.cs
│       │   ├── ShowsController.cs
│       │   └── EpisodesAndUserController.cs
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
        ├── App.tsx 
        ├── index.css
        ├── types/index.ts
        ├── services/api.ts
        ├── store/authStore.ts
        ├── components/
        │   ├── Layout.tsx
        │   ├── ShowCard.tsx
        │   └── StarRating.tsx
        └── pages/
            ├── HomePage.tsx
            ├── BrowsePage.tsx
            ├── ShowDetailPage.tsx
            ├── MyListPage.tsx
            ├── ProfilePage.tsx
            ├── LoginPage.tsx
            └── RegisterPage.tsx
```

---

## Features

### User Authentication
- JWT-based registration & login (ASP.NET Identity)
- Persistent sessions via localStorage
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

## Quick Start — Docker

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

## Manual Development Setup

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