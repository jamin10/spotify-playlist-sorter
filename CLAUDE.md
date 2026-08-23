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

## Architecture principles

- We follow domain-driven design and clean architecture. Domain logic and rules belong in `SpotifyPlaylistSorter.Domain` entities/value objects, not scattered across services.
- Dependencies point inward: `Domain` has no dependency on `Business` or outer layers; `Business` depends on `Domain` abstractions, not on infrastructure concretions (EF Core, external APIs) directly — those are accessed through interfaces (e.g. `ITrackStore`, `ISpotifyService`).
- Model behavior on domain entities rather than pushing all logic into services — favor rich domain models over anemic ones.

## Architecture notes

- `PlaylistAnalyserService` is the core service. It uses `ITrackStore` (not `AppDbContext` directly) so it can be unit tested without EF Core.
- `ISpotifyService.GetTrackAsync` / `GetPlaylistAsync` return plain POCOs — `SpotifyAPI.Web` concrete types do not leave `SpotifyService`.
- `TrackEntityMapper.ToTrack` is a pure static mapper (DTO → domain entity), independently testable with no mocking.
- `TrackAnalysisWorker` registers `ITrackStore` and `ISpotifyService` as transient.
- `ISpotifyService` (catalog: `GetTrackAsync`/`GetPlaylistAsync`, used only by the Worker) and `ISpotifyUserContext` (current-user data, used only by the Web app) both get their `SpotifyClient` from an `ISpotifyCredentialProvider` seam — a session-backed adapter (`SessionSpotifyCredentialProvider`, scoped, Web) or a client-credentials adapter (`ClientCredentialsProvider`, singleton, Worker). Neither façade holds a mutable client field itself.
- The Web-only "is the user logged in" check lives on `ISpotifySessionAuth.IsAuthenticated`, enforced via the `[RequireSpotifyLogin]` MVC filter (`SpotifyPlaylistSorterWeb/Filters`) rather than duplicated per call site.

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

## Agent skills

### Issue tracker

GitHub Issues (jamin10/spotify-playlist-sorter), via the gh CLI. See `docs/agents/issue-tracker.md`.

### Triage labels

Default five canonical labels (needs-triage, needs-info, ready-for-agent, ready-for-human, wontfix). See `docs/agents/triage-labels.md`.

### Domain docs

Single-context: `CONTEXT.md` + `docs/adr/` at the repo root. See `docs/agents/domain.md`.
