# Research: Current Layer Functionality — Spotify Playlist Sorter

**Date**: 2026-04-06T16:33:00Z
**Git Commit**: 6a70d59c63d0a5ebac02c554c4e095552496e53a
**Branch**: develop
**Repository**: spotify-playlist-sorter

## Research Question
What functionality currently lives in each layer of the solution, as a baseline for refactoring to clean architecture?

## Summary

The solution is a 7-project .NET 9 monorepo. Responsibilities are spread across two application hosts (a web app and a worker), a business layer, a domain/persistence layer, and small shared libraries. The Business project acts as both application logic and infrastructure — it directly references EF Core, RabbitMQ, Spotify SDK, and Cyanite HTTP clients, as well as owning ViewModels that are presentation concerns. The Domain project has a generic repository that is unused in practice. The Infrastructure project is an empty placeholder.

---

## Layer-by-Layer Findings

---

### 1. SpotifyPlaylistSorter.Domain

**Entities** ([SpotifyPlaylistSorter.Domain/Models/](SpotifyPlaylistSorter.Domain/Models/)):
- `Track` — `SpotifyTrackId`, `Title`, FK to `Album`, M:M to `Artist` and `Playlist`, owned `TrackAudioFeatures`
- `Artist` — `SpotifyArtistId`, `Name`, M:M to `Track` and `Album`
- `Album` — `SpotifyId`, `Name`, M:M to `Artist`, 1:M to `Track`
- `Playlist` — `SpotifyPlaylistId`, `Name`, `Description`, `ImageUrl`, M:M to `Track`
- `TrackAudioFeatures` — owned value object on `Track` (shadow columns): `EnergyLevel`, `EnergyDynamics`, `Bpm`, `BpmRangeAdjusted` (populated from Cyanite API)

**DbContext** ([SpotifyPlaylistSorter.Domain/AppDbContext.cs](SpotifyPlaylistSorter.Domain/AppDbContext.cs)):
- Exposes `DbSet<Playlist>`, `DbSet<Track>`, `DbSet<Artist>`, `DbSet<Album>`
- Configures unique indexes on all SpotifyId fields, M:M join tables (PlaylistTrack, TrackArtist, AlbumArtist with cascade delete), and owned `TrackAudioFeatures` as shadow columns with `AudioFeatures_` prefix
- PostgreSQL via Npgsql

**Generic Repository** ([SpotifyPlaylistSorter.Domain/Repositories/](SpotifyPlaylistSorter.Domain/Repositories/)):
- `IRepository<T>` — GetByIdAsync, GetAllAsync, FindAsync, AddAsync, Update, Remove, SaveChangesAsync
- `Repository<T>` — EF Core implementation
- **Not consumed by any service** — `ITrackStore` (in Business) is what's actually used

**Design-time factory** ([SpotifyPlaylistSorter.Domain/AppDbContextFactory.cs](SpotifyPlaylistSorter.Domain/AppDbContextFactory.cs)):
- Reads config from `TrackAnalysisWorker/appsettings.json` for `dotnet ef migrations`

**Migrations** ([SpotifyPlaylistSorter.Domain/Migrations/](SpotifyPlaylistSorter.Domain/Migrations/)):
- `20260401184621_InitialDomain` — creates all tables, indexes, FK constraints

**NuGet:** `Npgsql.EntityFrameworkCore.PostgreSQL`, `Microsoft.EntityFrameworkCore.Design`, `Microsoft.Extensions.Configuration.Json`

---

### 2. SpotifyPlaylistSorter.Business

This project contains: application logic, external API clients, DTOs, mappers, ViewModels, and the persistence abstraction. It directly references EF Core, Spotify SDK, Cyanite HTTP client, and RabbitMQ.

#### Services & Interfaces

| Interface | Implementation | Responsibility |
|-----------|---------------|----------------|
| `IAnalyserService` | `PlaylistAnalyserService` | Core orchestrator: loads existing tracks, fetches from Spotify + Cyanite, maps to entities, persists |
| `ISpotifyService` | `SpotifyService` | Spotify OAuth (code exchange + client-credentials), track/playlist fetch — returns DTOs only, hides `SpotifyAPI.Web` types from callers |
| `ICyaniteService` | `CyaniteService` | Executes GraphQL query against Cyanite, deserializes to `CyaniteTrack` model |
| `ICyaniteClient` | `CyaniteClient` | Low-level HTTP POST to `https://api.cyanite.ai/graphql`, returns raw JSON |
| `ITrackStore` | `TrackStore` | Persistence abstraction over `AppDbContext` — GetExistingTracks, FindPlaylist/Artist/Album, Add, SaveChanges |
| `IPlaylistsService` | `PlaylistsService` | Pagination for playlist/track UI views; batches and queues track IDs for analysis |
| `IUserProfileService` | `UserProfileService` | Fetches current Spotify user, maps to `CurrentUserViewModel` |
| `IMessageQueueService` | `RabbitMQService` | Publishes JSON to RabbitMQ queue `"message"` on `localhost`; creates a new connection per call |

#### PlaylistAnalyserService — data flow
([SpotifyPlaylistSorter.Business/Services/PlaylistAnalyserService.cs](SpotifyPlaylistSorter.Business/Services/PlaylistAnalyserService.cs))

1. `ITrackStore.GetExistingTracksAsync()` — loads tracks already in DB (skip re-analysis)
2. `ISpotifyService.GetTrackAsync()` → `SpotifyTrackDto`
3. `ICyaniteService.GetTrackAnalysis()` → `CyaniteTrack` (GraphQL response model)
4. Combined into `AnalysedTrackDto`
5. `TrackEntityMapper.ToTrack()` → `Domain.Models.Track`
6. `ITrackStore.Add()` + `SaveChangesAsync()` to persist
7. Maintains in-memory caches for Album and Artist resolution within a single run

#### TrackStore
([SpotifyPlaylistSorter.Business/Services/TrackStore.cs](SpotifyPlaylistSorter.Business/Services/TrackStore.cs))
- Directly injects `AppDbContext` from Domain
- Uses EF Core `.Include()` for eager loading of related entities
- Only place `AppDbContext` is used in production code

#### SpotifyService
([SpotifyPlaylistSorter.Business/Services/SpotifyService.cs](SpotifyPlaylistSorter.Business/Services/SpotifyService.cs))
- Holds `SpotifyClient` as a mutable property (set post-OAuth)
- Uses `IHttpContextAccessor` for session-based token storage (web context)
- Auto-authenticates with client credentials if client not set (worker context)
- Reads `SpotifyCredentials:ClientId/ClientSecret/RedirectUri` from `IConfiguration`

#### PlaylistsService
([SpotifyPlaylistSorter.Business/Services/PlaylistsService.cs](SpotifyPlaylistSorter.Business/Services/PlaylistsService.cs))
- Calls `SpotifyClient` API directly (bypasses `GetTrackAsync` DTO wrapper) for browse operations
- `GetAllTrackIds()` uses `.Result` (sync-over-async)
- Batches 10 track IDs per `AnalysePlaylist` message

#### DTOs ([SpotifyPlaylistSorter.Business/Dtos/](SpotifyPlaylistSorter.Business/Dtos/))
- `SpotifyTrackDto` — record: SpotifyTrackId, Title, AlbumModelDto, List\<ArtistDto\>
- `SpotifyPlaylistDto` — record: SpotifyPlaylistId, Name, Description, ImageUrl
- `AnalysedTrackDto` — class: combines SpotifyTrackDto + Cyanite audio features
- `AudioFeatures` (file: `AudioFeaturesDto.cs`) — EnergyLevel, EnergyDynamics, Bpm, BpmRangeAdjusted
- `ArtistDto` — SpotifyArtistId, Name
- `AlbumModelDto` (file: `AlbumDto.cs`) — SpotifyId, Name, List\<ArtistDto\>

#### Cyanite Response Models ([SpotifyPlaylistSorter.Business/Models/Cyanite/](SpotifyPlaylistSorter.Business/Models/Cyanite/))
Nested hierarchy for JSON deserialization:
`CyaniteTrack → Data → SpotifyTrack → AudioAnalysisV6 → AudioAnalysisResult → BpmPrediction`

#### ViewModels ([SpotifyPlaylistSorter.Business/Models/](SpotifyPlaylistSorter.Business/Models/))
- `SpotifyBaseViewModel` — `IsLoggedIn` bool
- `PlaylistsViewModel : SpotifyBaseViewModel` — `PaginatedList<FullPlaylistModel>`
- `FullPlaylistViewModel : SpotifyBaseViewModel` — `PaginatedList<TrackModel>`
- `CurrentUserViewModel : SpotifyAPI.Web.PrivateUser` — adds `IsLoggedIn`
- `FullPlaylistModel`, `TrackModel` — UI display shapes
- `PaginatedList<T> : List<T>, IPaginatedList` — pagination with routing metadata

#### Static Mapper ([SpotifyPlaylistSorter.Business/Mappers/TrackEntityMapper.cs](SpotifyPlaylistSorter.Business/Mappers/TrackEntityMapper.cs))
- `ToTrack(AnalysedTrackDto, Album, List<Artist>)` → `Domain.Models.Track`
- Pure static method, no external dependencies

**NuGet:** `SpotifyAPI.Web`, `SpotifyAPI.Web.Auth`, `AutoMapper`, `RabbitMQ.Client`, `Microsoft.EntityFrameworkCore`, `Microsoft.AspNetCore.Http`, `Microsoft.Extensions.Configuration`, `Microsoft.Extensions.Logging.Abstractions`

---

### 3. SpotifyPlaylistSorterWeb

MVC web application — handles Spotify OAuth entry, playlist browsing UI, and analysis dispatch.

#### Controllers ([SpotifyPlaylistSorterWeb/Controllers/](SpotifyPlaylistSorterWeb/Controllers/))

| Controller | Endpoints | Business services used |
|-----------|-----------|------------------------|
| `HomeController` | `GET /` | `IUserProfileService` |
| `SpotifyController` | `GET /Spotify/Login`, `GET /Spotify/Callback?code=` | `ISpotifyService` |
| `PlaylistsController` | `GET /Playlists/Current?page&pageSize`, `GET /Playlists/ViewPlaylist/{id}` | `IPlaylistsService` |
| `MessageQueueController` | `POST /MessageQueue/AnalysePlaylist` (AJAX, anti-forgery) | `IPlaylistsService` |

#### Spotify OAuth flow
1. User visits `/Spotify/Login` → `ISpotifyService.GetLoginUri()` → redirect to Spotify
2. Spotify redirects to `/Spotify/Callback?code=...` → `ISpotifyService.CreateSpotifyClient(code)` → redirect to `/`

#### Views ([SpotifyPlaylistSorterWeb/Views/](SpotifyPlaylistSorterWeb/Views/))
- `Home/Index.cshtml` — profile card (if logged in) or "Connect to Spotify" button
- `Playlists/Current.cshtml` — Bootstrap table of playlists, `_PaginationPartial`
- `Playlists/ViewPlaylist.cshtml` — track table, "Analyse" button triggering JS AJAX, `_PaginationPartial`, `_ArtistsPartial`
- `Shared/_Layout.cshtml` — Bootstrap 5 (bootswatch) dark navbar, jQuery
- `Shared/_LoginPartial.cshtml` — ASP.NET Identity login/logout
- `Shared/_PaginationPartial.cshtml`, `_ArtistsPartial.cshtml`

#### AutoMapper profile ([SpotifyPlaylistSorterWeb/Mappers/MappingProfile.cs](SpotifyPlaylistSorterWeb/Mappers/MappingProfile.cs))
Maps `SpotifyAPI.Web` types → Business ViewModels:
- `Paging<>` → `PaginatedList<>`
- `PrivateUser` → `CurrentUserViewModel`
- `FullTrack` / `IPlayableItem` → `TrackModel`
- `SimpleArtist` → `ArtistDto`
- `PlaylistTrack<IPlayableItem>` → `TrackModel`
- `FullPlaylist` → `FullPlaylistModel`

#### Data ([SpotifyPlaylistSorterWeb/Data/](SpotifyPlaylistSorterWeb/Data/))
- `ApplicationDbContext : IdentityDbContext` — ASP.NET Identity tables only (separate from domain `AppDbContext`)
- Migration: `00000000000000_CreateIdentitySchema`

#### JavaScript ([SpotifyPlaylistSorterWeb/wwwroot/js/site.js](SpotifyPlaylistSorterWeb/wwwroot/js/site.js))
- `queuePlaylist(playlistId, antiForgeryToken)` — `fetch` POST to `/MessageQueue/AnalysePlaylist` with CSRF token

#### Service registration (Program.cs)
- `ISpotifyService` → Singleton; `IUserProfileService`, `IPlaylistsService`, `IMessageQueueService` → Scoped
- Both `ApplicationDbContext` (Identity) and `AppDbContext` (Domain) registered with PostgreSQL
- Session: 30-minute timeout, HTTP-only cookie, GDPR-essential

#### Web-layer models
Files exist in [SpotifyPlaylistSorterWeb/Models/](SpotifyPlaylistSorterWeb/Models/) but are all **commented out** — the web project uses ViewModels from `SpotifyPlaylistSorter.Business.Models` directly.

**NuGet:** `SpotifyAPI.Web`, `SpotifyAPI.Web.Auth`, `AutoMapper.Extensions.Microsoft.DependencyInjection`, `RabbitMQ.Client`, `Microsoft.AspNetCore.Identity.*`, `Microsoft.EntityFrameworkCore.SqlServer` (present but Npgsql used at runtime)

---

### 4. TrackAnalysisWorker

.NET Worker Service — RabbitMQ consumer that drives the analysis pipeline.

**Worker** ([TrackAnalysisWorker/Worker.cs](TrackAnalysisWorker/Worker.cs)):
- `BackgroundService` connecting to RabbitMQ on `localhost`, queue `"message"` (hardcoded)
- Deserializes JSON body → `AnalysePlaylist`
- Creates a DI scope per message, resolves `IAnalyserService`, calls `.Analyse(message)`
- Manual `BasicAck` after processing

**DI registration** ([TrackAnalysisWorker/Program.cs](TrackAnalysisWorker/Program.cs)):
- `AppDbContext` → PostgreSQL via `DefaultConnection`
- `ICyaniteClient` → named `HttpClient` with base address `https://api.cyanite.ai/graphql` + Bearer token
- `ICyaniteService`, `ITrackStore`, `IAnalyserService` → Transient
- `ISpotifyService` → Singleton
- `IHttpContextAccessor` registered (required by `SpotifyService`)

**NuGet:** `Microsoft.Extensions.Hosting`, `Microsoft.Extensions.Http`, `Npgsql.EntityFrameworkCore.PostgreSQL`, `RabbitMQ.Client`

---

### 5. Common

Shared queue message contract between web (producer) and worker (consumer). No NuGet dependencies.

**Files** ([Common/Models/QueueMessages/](Common/Models/QueueMessages/)):
- `MessageBase` — `Type` (`QueueMessageTypeEnum`)
- `AnalysePlaylist : MessageBase` — `PlaylistId` (string), `TrackIds` (List\<string\>)
- `QueueMessageTypeEnum` — `Default = 0`, `AnalysePlaylist = 1`

---

### 6. SpotifyPlaylistSorter.Infrastructure

Contains only an empty `Class1` stub ([SpotifyPlaylistSorter.Infrastructure/Class1.cs](SpotifyPlaylistSorter.Infrastructure/Class1.cs)). No functionality implemented. No NuGet dependencies. No project references from other projects.

---

### 7. SpotifyPlaylistSorter.Business.Tests

Unit tests using MSTest + Moq. Parallel execution enabled at method level.

**PlaylistAnalyserServiceTests** (5 tests):
- Mocks: `ICyaniteService`, `ISpotifyService`, `ITrackStore`, `ILogger<PlaylistAnalyserService>`
- Scenarios: skips existing tracks, creates new playlists, reuses existing playlists, logs warnings on API failure, associates all tracks with playlist

**TrackEntityMapperTests** (5 tests):
- No mocks (pure static)
- Covers: SpotifyTrackId, Title, Album, Artists, AudioFeatures mappings

**Not tested:** `SpotifyService`, `CyaniteService`, `TrackStore`, `PlaylistsService`, `UserProfileService`, `RabbitMQService`, all controllers, the worker, AutoMapper profiles

---

## Dependency Graph

```
SpotifyPlaylistSorterWeb
    ├── SpotifyPlaylistSorter.Business
    │   ├── SpotifyPlaylistSorter.Domain   ← AppDbContext consumed by TrackStore
    │   └── Common
    ├── SpotifyPlaylistSorter.Domain
    └── Common

TrackAnalysisWorker
    ├── SpotifyPlaylistSorter.Business
    │   ├── SpotifyPlaylistSorter.Domain
    │   └── Common
    ├── SpotifyPlaylistSorter.Domain
    └── Common

SpotifyPlaylistSorter.Business.Tests
    └── SpotifyPlaylistSorter.Business

SpotifyPlaylistSorter.Infrastructure  (isolated, no references)
```

---

## Key File Index

| File | What's there |
|------|-------------|
| [SpotifyPlaylistSorter.Domain/AppDbContext.cs](SpotifyPlaylistSorter.Domain/AppDbContext.cs) | EF Core DbContext, all entity configurations |
| [SpotifyPlaylistSorter.Domain/Models/Track.cs](SpotifyPlaylistSorter.Domain/Models/Track.cs) | Track entity with owned AudioFeatures |
| [SpotifyPlaylistSorter.Domain/Repositories/IRepository.cs](SpotifyPlaylistSorter.Domain/Repositories/IRepository.cs) | Generic repository interface (defined but unused) |
| [SpotifyPlaylistSorter.Business/Services/PlaylistAnalyserService.cs](SpotifyPlaylistSorter.Business/Services/PlaylistAnalyserService.cs) | Core analysis orchestrator |
| [SpotifyPlaylistSorter.Business/Services/TrackStore.cs](SpotifyPlaylistSorter.Business/Services/TrackStore.cs) | Only EF Core consumer in production — wraps AppDbContext |
| [SpotifyPlaylistSorter.Business/Services/SpotifyService.cs](SpotifyPlaylistSorter.Business/Services/SpotifyService.cs) | Spotify OAuth + API calls |
| [SpotifyPlaylistSorter.Business/Services/CyaniteService.cs](SpotifyPlaylistSorter.Business/Services/CyaniteService.cs) | Cyanite GraphQL queries |
| [SpotifyPlaylistSorter.Business/Services/PlaylistsService.cs](SpotifyPlaylistSorter.Business/Services/PlaylistsService.cs) | Playlist browsing + analysis queue dispatch |
| [SpotifyPlaylistSorter.Business/Services/RabbitMQService.cs](SpotifyPlaylistSorter.Business/Services/RabbitMQService.cs) | RabbitMQ publisher |
| [SpotifyPlaylistSorter.Business/Mappers/TrackEntityMapper.cs](SpotifyPlaylistSorter.Business/Mappers/TrackEntityMapper.cs) | Static DTO→entity mapper |
| [SpotifyPlaylistSorter.Business/Models/](SpotifyPlaylistSorter.Business/Models/) | ViewModels + Cyanite response models |
| [SpotifyPlaylistSorter.Business/Dtos/](SpotifyPlaylistSorter.Business/Dtos/) | API-facing DTOs |
| [SpotifyPlaylistSorterWeb/Controllers/](SpotifyPlaylistSorterWeb/Controllers/) | 4 MVC controllers |
| [SpotifyPlaylistSorterWeb/Mappers/MappingProfile.cs](SpotifyPlaylistSorterWeb/Mappers/MappingProfile.cs) | AutoMapper: SpotifyAPI.Web types → ViewModels |
| [SpotifyPlaylistSorterWeb/Data/ApplicationDbContext.cs](SpotifyPlaylistSorterWeb/Data/ApplicationDbContext.cs) | ASP.NET Identity DbContext |
| [TrackAnalysisWorker/Worker.cs](TrackAnalysisWorker/Worker.cs) | RabbitMQ consumer loop |
| [TrackAnalysisWorker/Program.cs](TrackAnalysisWorker/Program.cs) | Worker DI composition root |
| [Common/Models/QueueMessages/AnalysePlaylist.cs](Common/Models/QueueMessages/AnalysePlaylist.cs) | Queue message contract |
| [SpotifyPlaylistSorter.Infrastructure/Class1.cs](SpotifyPlaylistSorter.Infrastructure/Class1.cs) | Empty placeholder |
