# spotify-playlist-sorter

## Project overview

.NET 9 solution that analyses Spotify playlists using the Spotify and Cyanite APIs, storing audio feature data (energy level, BPM, etc.) in a PostgreSQL database. The analysis results are used to sort and group tracks.

## Projects

- **SpotifyPlaylistSorter.Business** — services, DTOs, mappers, external API clients
- **SpotifyPlaylistSorter.Domain** — EF Core entities, `AppDbContext`, migrations
- **SpotifyPlaylistSorter.Business.Tests** — MSTest + Moq unit tests for the Business layer
- **TrackAnalysisWorker** — background worker consuming RabbitMQ messages, calls `IAnalyserService`
- **SpotifyPlaylistSorterWeb** — ASP.NET Core web app, queues analysis messages via Spotify OAuth
- **Common** — shared queue message models
- **SpotifyPlaylistSorter.Infrastructure** — infrastructure helpers

## Architecture notes

- `PlaylistAnalyserService` is the core service. It uses `ITrackStore` (not `AppDbContext` directly) so it can be unit tested without EF Core.
- `ISpotifyService.GetTrackAsync` / `GetPlaylistAsync` return plain POCOs — `SpotifyAPI.Web` concrete types do not leave `SpotifyService`.
- `TrackEntityMapper.ToTrack` is a pure static mapper (DTO → domain entity), independently testable with no mocking.
- `TrackAnalysisWorker` registers `ITrackStore` as transient; `ISpotifyService` is singleton.

## Commands

### Build
```
dotnet build
```

### Run tests
```
dotnet test SpotifyPlaylistSorter.Business.Tests/
```

### Run worker
```
dotnet run --project TrackAnalysisWorker/
```

### Run web app
```
dotnet run --project SpotifyPlaylistSorterWeb/
```

### Add a migration
```
dotnet ef migrations add <MigrationName> --project SpotifyPlaylistSorter.Domain --startup-project TrackAnalysisWorker
```

## Coding conventions

- Use private methods with descriptive names over long methods with step comments.
- Keep `PlaylistAnalyserService` methods single-purpose — `Analyse` is a coordinator only.
- New persistence operations belong in `TrackStore`, not in service classes.
- Prefer MSTest for new tests; use Moq for mocking interfaces.
