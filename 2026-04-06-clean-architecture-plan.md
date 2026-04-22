# Clean Architecture Refactoring Plan

## Overview

Restructure the Spotify Playlist Sorter solution so each project has a single, well-defined responsibility following clean architecture:

| Layer | Project | Depends on |
|-------|---------|------------|
| **Domain** | `SpotifyPlaylistSorter.Domain` | Nothing |
| **Business** | `SpotifyPlaylistSorter.Business` | Domain |
| **Infrastructure** | `SpotifyPlaylistSorter.Infrastructure` | Domain, Business |
| **Web** | `SpotifyPlaylistSorterWeb` | Domain, Business, Infrastructure |
| **Worker** | `TrackAnalysisWorker` | Domain, Business, Infrastructure |
| **Tests** | `SpotifyPlaylistSorter.Business.Tests` | Domain, Business |

The `Common` project is absorbed into `Domain`.

## Current State Analysis

The Business project currently acts as both application logic **and** infrastructure — it directly owns EF Core consumers (`TrackStore`), external API clients (`SpotifyService`, `CyaniteService`, `CyaniteClient`), message queue implementations (`RabbitMQService`), and presentation-layer ViewModels. The Infrastructure project is an empty placeholder. The Domain project owns `AppDbContext` which should be an infrastructure concern.

### Key Discoveries:
- `TrackStore` ([SpotifyPlaylistSorter.Business/Services/TrackStore.cs](SpotifyPlaylistSorter.Business/Services/TrackStore.cs)) is the only place `AppDbContext` is consumed in production code
- `PlaylistsService` ([SpotifyPlaylistSorter.Business/Services/PlaylistsService.cs](SpotifyPlaylistSorter.Business/Services/PlaylistsService.cs)) accesses `_spotifyService.SpotifyClient` directly (leaks SpotifyAPI.Web types) — fixing this is out of scope
- `ISpotifyService` ([SpotifyPlaylistSorter.Business/Services/ISpotifyService.cs](SpotifyPlaylistSorter.Business/Services/ISpotifyService.cs)) exposes `SpotifyClient` property typed to SpotifyAPI.Web — Business will keep the SpotifyAPI.Web package reference for this interface type
- ViewModels in Business ([SpotifyPlaylistSorter.Business/Models/](SpotifyPlaylistSorter.Business/Models/)) are presentation concerns: `PlaylistsViewModel`, `FullPlaylistViewModel`, `CurrentUserViewModel`, `SpotifyBaseViewModel`, `ErrorViewModel`
- `CurrentUserViewModel` extends `SpotifyAPI.Web.PrivateUser` — needs replacing with a standalone DTO
- `PaginatedList<T>` combines data and UI routing metadata (`Action`, `RouteValues`) — this is a presentation concern
- Domain's `IRepository<T>` ([SpotifyPlaylistSorter.Domain/Repositories/IRepository.cs](SpotifyPlaylistSorter.Domain/Repositories/IRepository.cs)) is unused — keep the interface in Domain, move implementation to Infrastructure
- `MappingProfile` ([SpotifyPlaylistSorterWeb/Mappers/MappingProfile.cs](SpotifyPlaylistSorterWeb/Mappers/MappingProfile.cs)) maps SpotifyAPI.Web types → ViewModels/DTOs — stays in Web
- The EF migration ([SpotifyPlaylistSorter.Domain/Migrations/](SpotifyPlaylistSorter.Domain/Migrations/)) needs to move with `AppDbContext` to Infrastructure

## Desired End State

After this plan is complete:

```
SpotifyPlaylistSorter.Domain/
  ├── Models/              ← Entities (Track, Artist, Album, Playlist, TrackAudioFeatures)
  │   └── QueueMessages/   ← Absorbed from Common (AnalysePlaylist, MessageBase, QueueMessageTypeEnum)
  └── Repositories/        ← IRepository<T>, ITrackStore

SpotifyPlaylistSorter.Business/
  ├── Services/            ← Interfaces + implementations that are pure business logic
  │   ├── IAnalyserService.cs, PlaylistAnalyserService.cs
  │   ├── ISpotifyService.cs          (interface only)
  │   ├── ICyaniteService.cs          (interface only)
  │   ├── IMessageQueueService.cs     (interface only)
  │   ├── IPlaylistsService.cs        (refactored: returns DTOs)
  │   ├── PlaylistsService.cs         (refactored: returns DTOs)
  │   ├── IUserProfileService.cs      (refactored: returns DTO)
  │   └── UserProfileService.cs       (refactored: returns DTO)
  ├── Clients/Interfaces/  ← ICyaniteClient (interface only)
  ├── Dtos/                ← All DTOs (existing + new: UserProfileDto, PlaylistSummaryDto, TrackSummaryDto)
  ├── Mappers/             ← TrackEntityMapper (static, pure)
  └── Models/Cyanite/      ← Cyanite GraphQL response models (part of ICyaniteService contract)

SpotifyPlaylistSorter.Infrastructure/
  ├── Persistence/
  │   ├── AppDbContext.cs
  │   ├── AppDbContextFactory.cs
  │   ├── TrackStore.cs              ← ITrackStore implementation
  │   └── Repository.cs             ← IRepository<T> implementation
  ├── Migrations/                    ← EF Core migrations
  ├── ExternalServices/
  │   ├── SpotifyService.cs          ← ISpotifyService implementation
  │   ├── CyaniteService.cs          ← ICyaniteService implementation
  │   └── CyaniteClient.cs          ← ICyaniteClient implementation
  ├── Messaging/
  │   └── RabbitMQService.cs         ← IMessageQueueService implementation
  └── DependencyInjection.cs         ← Extension method for DI registration

SpotifyPlaylistSorterWeb/
  ├── Controllers/         ← Same 4 controllers (updated to map DTOs → ViewModels)
  ├── Models/              ← ViewModels moved from Business
  │   ├── SpotifyBaseViewModel.cs
  │   ├── PlaylistsViewModel.cs
  │   ├── FullPlaylistViewModel.cs
  │   ├── CurrentUserViewModel.cs
  │   ├── ErrorViewModel.cs
  │   ├── PaginatedList.cs
  │   └── IPaginatedList.cs
  ├── Mappers/             ← MappingProfile (updated)
  └── Data/                ← ApplicationDbContext (Identity, unchanged)

TrackAnalysisWorker/       ← Updated DI, otherwise unchanged
```

### Verification:
- `dotnet build` succeeds with no errors
- `dotnet test SpotifyPlaylistSorter.Business.Tests/` — all 10 tests pass
- No project references to `Common`
- Domain has zero NuGet packages (no EF Core)
- Business has no EF Core, RabbitMQ, or Microsoft.AspNetCore.Http packages
- Infrastructure has EF Core, Npgsql, SpotifyAPI.Web, RabbitMQ.Client
- `grep -r "using SpotifyPlaylistSorter.Common" .` returns no results

## What We're NOT Doing

- **Not fixing the SpotifyClient leak**: `ISpotifyService.SpotifyClient` exposes `SpotifyAPI.Web.SpotifyClient` directly. Wrapping it properly would require adding multiple new methods to `ISpotifyService` for playlist browsing. This is a separate task.
- **Not refactoring PlaylistsService's sync-over-async**: `GetAllTrackIds()` uses `.Result` — out of scope.
- **Not changing service lifetimes**: Keeping existing Singleton/Scoped/Transient registrations.
- **Not adding new tests**: Only updating existing tests to compile.
- **Not changing the database**: No new migrations. Existing migration moves as-is.
- **Not touching Views/Razor**: Views only need `@using` namespace updates.

---

## Phase 1: Merge Common into Domain

### Overview
Move the 3 queue message types from `Common/` into `Domain/Models/QueueMessages/` and remove the Common project from the solution.

### Changes Required:

#### 1. Create queue messages directory in Domain
Create `SpotifyPlaylistSorter.Domain/Models/QueueMessages/`

#### 2. Move files and update namespaces

**`SpotifyPlaylistSorter.Domain/Models/QueueMessages/QueueMessageTypeEnum.cs`** (from `Common/Enums/QueueMessageTypeEnum.cs`):
```csharp
namespace SpotifyPlaylistSorter.Domain.Models.QueueMessages;

public enum QueueMessageTypeEnum
{
    Default = 0,
    AnalysePlaylist = 1,
}
```

**`SpotifyPlaylistSorter.Domain/Models/QueueMessages/MessageBase.cs`** (from `Common/Models/MessageBase.cs`):
```csharp
namespace SpotifyPlaylistSorter.Domain.Models.QueueMessages;

public class MessageBase
{
    public QueueMessageTypeEnum Type { get; set; } = QueueMessageTypeEnum.Default;
}
```

**`SpotifyPlaylistSorter.Domain/Models/QueueMessages/AnalysePlaylist.cs`** (from `Common/Models/AnalysePlaylist.cs`):
```csharp
namespace SpotifyPlaylistSorter.Domain.Models.QueueMessages;

public class AnalysePlaylist : MessageBase
{
    public string PlaylistId { get; set; }
    public List<string> TrackIds { get; set; } = new List<string>();

    public AnalysePlaylist(string playlistId, List<string> trackIds)
    {
        Type = QueueMessageTypeEnum.AnalysePlaylist;
        PlaylistId = playlistId;
        TrackIds = trackIds ?? new List<string>();
    }
}
```

#### 3. Update all `using` statements referencing Common

Find and replace across the solution:
- `using SpotifyPlaylistSorter.Common.Models.QueueMessages` → `using SpotifyPlaylistSorter.Domain.Models.QueueMessages`
- `using SpotifyPlaylistSorter.Common.Models.Enums` → `using SpotifyPlaylistSorter.Domain.Models.QueueMessages` (enum is in same namespace now)

**Files affected:**
- `SpotifyPlaylistSorter.Business/Services/IAnalyserService.cs`
- `SpotifyPlaylistSorter.Business/Services/PlaylistAnalyserService.cs`
- `SpotifyPlaylistSorter.Business/Services/PlaylistsService.cs`
- `TrackAnalysisWorker/Worker.cs`

#### 4. Remove Common project references from all .csproj files

Remove `<ProjectReference Include="../Common/Common.csproj" />` from:
- `SpotifyPlaylistSorter.Business/SpotifyPlaylistSorter.Business.csproj`
- `SpotifyPlaylistSorterWeb/SpotifyPlaylistSorterWeb.csproj`
- `TrackAnalysisWorker/TrackAnalysisWorker.csproj`
- `SpotifyPlaylistSorter.Business.Tests/SpotifyPlaylistSorter.Business.Tests.csproj`

#### 5. Remove Common project from solution
- `dotnet sln remove Common/Common.csproj`
- Delete `Common/` directory (source files only — the files have been moved to Domain)

### Success Criteria:

#### Automated Verification:
- [ ] Solution builds: `dotnet build`
- [ ] Tests pass: `dotnet test SpotifyPlaylistSorter.Business.Tests/`
- [ ] No references to Common remain: `grep -r "Common.csproj" --include="*.csproj" .` returns nothing
- [ ] No old namespace imports: `grep -r "SpotifyPlaylistSorter.Common" --include="*.cs" .` returns nothing (excluding obj/ and bin/)

---

## Phase 2: Move Persistence to Infrastructure, ITrackStore to Domain

### Overview
Set up the Infrastructure project with EF Core, move `AppDbContext`/migrations/repository implementations from Domain → Infrastructure, move `ITrackStore` from Business → Domain, and move `TrackStore` from Business → Infrastructure.

### Changes Required:

#### 1. Configure Infrastructure .csproj

**`SpotifyPlaylistSorter.Infrastructure/SpotifyPlaylistSorter.Infrastructure.csproj`:**
```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.EntityFrameworkCore" Version="9.0.4" />
    <PackageReference Include="Microsoft.EntityFrameworkCore.Design" Version="9.0.4" />
    <PackageReference Include="Microsoft.Extensions.Configuration" Version="9.0.4" />
    <PackageReference Include="Microsoft.Extensions.Configuration.Json" Version="9.0.4" />
    <PackageReference Include="Npgsql.EntityFrameworkCore.PostgreSQL" Version="9.0.4" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="../SpotifyPlaylistSorter.Domain/SpotifyPlaylistSorter.Domain.csproj" />
    <ProjectReference Include="../SpotifyPlaylistSorter.Business/SpotifyPlaylistSorter.Business.csproj" />
  </ItemGroup>

</Project>
```

#### 2. Delete empty Class1.cs
Delete `SpotifyPlaylistSorter.Infrastructure/Class1.cs`

#### 3. Create Persistence directory structure
```
SpotifyPlaylistSorter.Infrastructure/
  └── Persistence/
```

#### 4. Move AppDbContext to Infrastructure

Move `SpotifyPlaylistSorter.Domain/AppDbContext.cs` → `SpotifyPlaylistSorter.Infrastructure/Persistence/AppDbContext.cs`

Update namespace: `SpotifyPlaylistSorter.Domain` → `SpotifyPlaylistSorter.Infrastructure.Persistence`

```csharp
using Microsoft.EntityFrameworkCore;
using SpotifyPlaylistSorter.Domain.Models;

namespace SpotifyPlaylistSorter.Infrastructure.Persistence;

public class AppDbContext : DbContext
{
    // ... same implementation, same OnModelCreating
}
```

#### 5. Move AppDbContextFactory to Infrastructure

Move `SpotifyPlaylistSorter.Domain/AppDbContextFactory.cs` → `SpotifyPlaylistSorter.Infrastructure/Persistence/AppDbContextFactory.cs`

Update namespace: `SpotifyPlaylistSorter.Domain` → `SpotifyPlaylistSorter.Infrastructure.Persistence`

#### 6. Move Migrations to Infrastructure

Move entire directory: `SpotifyPlaylistSorter.Domain/Migrations/` → `SpotifyPlaylistSorter.Infrastructure/Migrations/`

Update namespace in all migration files from `SpotifyPlaylistSorter.Domain.Migrations` → `SpotifyPlaylistSorter.Infrastructure.Migrations`

Files to update:
- `InitialDomain.cs`
- `InitialDomain.Designer.cs`
- `AppDbContextModelSnapshot.cs`

In the Designer.cs and Snapshot files, update the `[DbContext(typeof(...))]` attribute to reference the new namespace.

#### 7. Move ITrackStore to Domain

Move `SpotifyPlaylistSorter.Business/Services/ITrackStore.cs` → `SpotifyPlaylistSorter.Domain/Repositories/ITrackStore.cs`

Update namespace: `SpotifyPlaylistSorter.Business.Services` → `SpotifyPlaylistSorter.Domain.Repositories`

```csharp
using SpotifyPlaylistSorter.Domain.Models;

namespace SpotifyPlaylistSorter.Domain.Repositories;

public interface ITrackStore
{
    Task<List<Track>> GetExistingTracksAsync(IEnumerable<string> spotifyTrackIds);
    Task<Playlist?> FindPlaylistAsync(string spotifyPlaylistId);
    Task<Artist?> FindArtistAsync(string spotifyArtistId);
    Task<Album?> FindAlbumAsync(string spotifyAlbumId);
    void Add<T>(T entity) where T : class;
    Task SaveChangesAsync();
}
```

Note: Remove the `DomainModels` alias since we're now in the Domain namespace.

#### 8. Move TrackStore to Infrastructure

Move `SpotifyPlaylistSorter.Business/Services/TrackStore.cs` → `SpotifyPlaylistSorter.Infrastructure/Persistence/TrackStore.cs`

Update namespace: `SpotifyPlaylistSorter.Business.Services` → `SpotifyPlaylistSorter.Infrastructure.Persistence`

```csharp
using Microsoft.EntityFrameworkCore;
using SpotifyPlaylistSorter.Domain.Models;
using SpotifyPlaylistSorter.Domain.Repositories;

namespace SpotifyPlaylistSorter.Infrastructure.Persistence;

public class TrackStore : ITrackStore
{
    private readonly AppDbContext _db;
    // ... same implementation, replace DomainModels alias with direct type references
}
```

#### 9. Move Repository<T> to Infrastructure

Move `SpotifyPlaylistSorter.Domain/Repositories/Repository.cs` → `SpotifyPlaylistSorter.Infrastructure/Persistence/Repository.cs`

Update namespace: `SpotifyPlaylistSorter.Domain.Repositories` → `SpotifyPlaylistSorter.Infrastructure.Persistence`

Keep implementing `SpotifyPlaylistSorter.Domain.Repositories.IRepository<T>`.

#### 10. Strip EF Core packages from Domain .csproj

**`SpotifyPlaylistSorter.Domain/SpotifyPlaylistSorter.Domain.csproj`:**
```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>

</Project>
```

Remove all PackageReference entries (EF Core Design, Configuration, Npgsql).

#### 11. Update using statements across the solution

Replace `using SpotifyPlaylistSorter.Domain;` (for AppDbContext) with `using SpotifyPlaylistSorter.Infrastructure.Persistence;` in:
- `SpotifyPlaylistSorterWeb/Program.cs`
- `TrackAnalysisWorker/Program.cs`

Replace `using SpotifyPlaylistSorter.Business.Services;` (for ITrackStore/TrackStore) with `using SpotifyPlaylistSorter.Domain.Repositories;` in:
- `SpotifyPlaylistSorter.Business/Services/PlaylistAnalyserService.cs`
- `TrackAnalysisWorker/Program.cs`

Remove `using SpotifyPlaylistSorter.Business.Services;` reference to TrackStore from:
- `TrackAnalysisWorker/Program.cs` (TrackStore now in Infrastructure namespace)

#### 12. Update CLAUDE.md migrations command
Change:
```
dotnet ef migrations add <MigrationName> --project SpotifyPlaylistSorter.Domain --startup-project TrackAnalysisWorker
```
To:
```
dotnet ef migrations add <MigrationName> --project SpotifyPlaylistSorter.Infrastructure --startup-project TrackAnalysisWorker
```

### Success Criteria:

#### Automated Verification:
- [ ] Solution builds: `dotnet build`
- [ ] Tests pass: `dotnet test SpotifyPlaylistSorter.Business.Tests/`
- [ ] Domain .csproj has zero PackageReference entries
- [ ] `AppDbContext` is in `SpotifyPlaylistSorter.Infrastructure.Persistence` namespace
- [ ] `ITrackStore` is in `SpotifyPlaylistSorter.Domain.Repositories` namespace
- [ ] `TrackStore` is in `SpotifyPlaylistSorter.Infrastructure.Persistence` namespace

---

## Phase 3: Move External Service Implementations to Infrastructure

### Overview
Move `SpotifyService`, `CyaniteService`, `CyaniteClient`, and `RabbitMQService` implementations from Business to Infrastructure. Their interfaces remain in Business.

### Changes Required:

#### 1. Create directory structure in Infrastructure
```
SpotifyPlaylistSorter.Infrastructure/
  ├── ExternalServices/
  └── Messaging/
```

#### 2. Add NuGet packages to Infrastructure .csproj

Add to `SpotifyPlaylistSorter.Infrastructure/SpotifyPlaylistSorter.Infrastructure.csproj`:
```xml
<PackageReference Include="Microsoft.AspNetCore.Http" Version="2.3.0" />
<PackageReference Include="Microsoft.Extensions.Logging.Abstractions" Version="9.0.9" />
<PackageReference Include="RabbitMQ.Client" Version="7.1.2" />
<PackageReference Include="SpotifyAPI.Web" Version="7.2.1" />
<PackageReference Include="SpotifyAPI.Web.Auth" Version="7.2.1" />
```

#### 3. Move SpotifyService

Move `SpotifyPlaylistSorter.Business/Services/SpotifyService.cs` → `SpotifyPlaylistSorter.Infrastructure/ExternalServices/SpotifyService.cs`

Update namespace: `SpotifyPlaylistSorter.Business.Services` → `SpotifyPlaylistSorter.Infrastructure.ExternalServices`

Add using: `using SpotifyPlaylistSorter.Business.Services;` (for `ISpotifyService`)
Add using: `using SpotifyPlaylistSorter.Business.Dtos;` (for DTO types)

#### 4. Move CyaniteService

Move `SpotifyPlaylistSorter.Business/Services/CyaniteService.cs` → `SpotifyPlaylistSorter.Infrastructure/ExternalServices/CyaniteService.cs`

Update namespace: `SpotifyPlaylistSorter.Business.Services` → `SpotifyPlaylistSorter.Infrastructure.ExternalServices`

Add using: `using SpotifyPlaylistSorter.Business.Services;` (for `ICyaniteService`)
Add using: `using SpotifyPlaylistSorter.Business.Clients.Interfaces;` (for `ICyaniteClient`)
Add using: `using SpotifyPlaylistSorter.Business.Models.Cyanite;` (for response models)

#### 5. Move CyaniteClient

Move `SpotifyPlaylistSorter.Business/Clients/Implementations/CyaniteClient.cs` → `SpotifyPlaylistSorter.Infrastructure/ExternalServices/CyaniteClient.cs`

Update namespace: `SpotifyPlaylistSorter.Business.Clients.Implementations` → `SpotifyPlaylistSorter.Infrastructure.ExternalServices`

Add using: `using SpotifyPlaylistSorter.Business.Clients.Interfaces;` (for `ICyaniteClient`)

#### 6. Move RabbitMQService

Move `SpotifyPlaylistSorter.Business/Services/RabbitMQService.cs` → `SpotifyPlaylistSorter.Infrastructure/Messaging/RabbitMQService.cs`

Update namespace: `SpotifyPlaylistSorter.Business.Services` → `SpotifyPlaylistSorter.Infrastructure.Messaging`

Add using: `using SpotifyPlaylistSorter.Business.Services;` (for `IMessageQueueService`)

#### 7. Clean up Business .csproj

Remove from `SpotifyPlaylistSorter.Business/SpotifyPlaylistSorter.Business.csproj`:
```xml
<!-- Remove these -->
<PackageReference Include="Microsoft.AspNetCore.Http" Version="2.3.0" />
<PackageReference Include="Microsoft.EntityFrameworkCore" Version="9.0.4" />
<PackageReference Include="RabbitMQ.Client" Version="7.1.2" />
<PackageReference Include="SpotifyAPI.Web.Auth" Version="7.2.1" />
```

Keep:
```xml
<PackageReference Include="AutoMapper" Version="12.0.1" />
<PackageReference Include="Microsoft.Extensions.Configuration" Version="9.0.9" />
<PackageReference Include="Microsoft.Extensions.Logging.Abstractions" Version="9.0.9" />
<PackageReference Include="SpotifyAPI.Web" Version="7.2.1" />
```

Note: `SpotifyAPI.Web` is kept because `ISpotifyService` exposes `SpotifyClient?` property typed to `SpotifyAPI.Web.SpotifyClient`. `AutoMapper` is kept because `PlaylistsService` and `UserProfileService` use it (until Phase 4). `Microsoft.Extensions.Configuration` is kept because service interfaces may need `IConfiguration`.

#### 8. Delete empty directories in Business

After moving files, delete if empty:
- `SpotifyPlaylistSorter.Business/Clients/Implementations/`

Keep:
- `SpotifyPlaylistSorter.Business/Clients/Interfaces/` (still has `ICyaniteClient.cs`)

#### 9. Update DI registrations

This is a partial update — full DI cleanup is in Phase 5. For now, update the `using` statements in Program.cs files to find the moved implementations:

**`TrackAnalysisWorker/Program.cs`:**
- Replace `using SpotifyPlaylistSorter.Business.Clients.Implementations;` → `using SpotifyPlaylistSorter.Infrastructure.ExternalServices;`
- Add `using SpotifyPlaylistSorter.Infrastructure.Persistence;` (for TrackStore from Phase 2)
- Add `using SpotifyPlaylistSorter.Infrastructure.ExternalServices;`

**`SpotifyPlaylistSorterWeb/Program.cs`:**
- Add `using SpotifyPlaylistSorter.Infrastructure.ExternalServices;`
- Add `using SpotifyPlaylistSorter.Infrastructure.Messaging;`

#### 10. Add Infrastructure project reference to Web and Worker

Add to `SpotifyPlaylistSorterWeb/SpotifyPlaylistSorterWeb.csproj`:
```xml
<ProjectReference Include="../SpotifyPlaylistSorter.Infrastructure/SpotifyPlaylistSorter.Infrastructure.csproj" />
```

Add to `TrackAnalysisWorker/TrackAnalysisWorker.csproj`:
```xml
<ProjectReference Include="../SpotifyPlaylistSorter.Infrastructure/SpotifyPlaylistSorter.Infrastructure.csproj" />
```

### Success Criteria:

#### Automated Verification:
- [ ] Solution builds: `dotnet build`
- [ ] Tests pass: `dotnet test SpotifyPlaylistSorter.Business.Tests/`
- [ ] No implementation classes remain in Business/Services/ except `PlaylistAnalyserService.cs`, `PlaylistsService.cs`, `UserProfileService.cs`
- [ ] Business .csproj has no `RabbitMQ.Client` or `Microsoft.EntityFrameworkCore` package references
- [ ] Infrastructure has `SpotifyService.cs`, `CyaniteService.cs`, `CyaniteClient.cs`, `RabbitMQService.cs`

---

## Phase 4: Move ViewModels to Web, Refactor Service Return Types

### Overview
Move presentation models from Business to Web. Refactor `IPlaylistsService` and `IUserProfileService` to return DTOs instead of ViewModels. Create new DTOs to replace ViewModel return types.

### Changes Required:

#### 1. Create new DTOs in Business

**`SpotifyPlaylistSorter.Business/Dtos/UserProfileDto.cs`** (new):
```csharp
namespace SpotifyPlaylistSorter.Business.Dtos;

public record UserProfileDto(
    string? DisplayName,
    string? ImageUrl,
    string? Id
);
```

**`SpotifyPlaylistSorter.Business/Dtos/PaginatedResultDto.cs`** (new):
```csharp
namespace SpotifyPlaylistSorter.Business.Dtos;

public class PaginatedResultDto<T>
{
    public List<T> Items { get; set; } = new();
    public int Total { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public string? Next { get; set; }
    public string? Previous { get; set; }
}
```

**`SpotifyPlaylistSorter.Business/Dtos/PlaylistSummaryDto.cs`** (new — replaces `FullPlaylistModel`):
```csharp
namespace SpotifyPlaylistSorter.Business.Dtos;

public class PlaylistSummaryDto
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public string? ImageUrl { get; set; }
    public string? TracksUrl { get; set; }
    public int? TracksTotal { get; set; }
    public string? SpotifyId { get; set; }
}
```

**`SpotifyPlaylistSorter.Business/Dtos/TrackSummaryDto.cs`** (new — replaces `TrackModel`):
```csharp
namespace SpotifyPlaylistSorter.Business.Dtos;

public class TrackSummaryDto
{
    public required string Name { get; set; }
    public required List<ArtistDto> Artists { get; set; }
    public required string SpotifyTrackId { get; set; }
}
```

#### 2. Refactor IUserProfileService and UserProfileService

**`SpotifyPlaylistSorter.Business/Services/IUserProfileService.cs`:**
```csharp
using SpotifyPlaylistSorter.Business.Dtos;

namespace SpotifyPlaylistSorter.Business.Services;

public interface IUserProfileService
{
    Task<UserProfileDto?> GetCurrentUserProfile();
}
```

**`SpotifyPlaylistSorter.Business/Services/UserProfileService.cs`:**
```csharp
using SpotifyPlaylistSorter.Business.Dtos;

namespace SpotifyPlaylistSorter.Business.Services;

public class UserProfileService : IUserProfileService
{
    private readonly ISpotifyService _spotifyService;

    public UserProfileService(ISpotifyService spotifyService)
    {
        _spotifyService = spotifyService;
    }

    public async Task<UserProfileDto?> GetCurrentUserProfile()
    {
        if (_spotifyService.SpotifyClient == null)
            return null;

        var userProfile = await _spotifyService.SpotifyClient.UserProfile.Current();
        if (userProfile == null)
            return null;

        return new UserProfileDto(
            DisplayName: userProfile.DisplayName,
            ImageUrl: userProfile.Images?.FirstOrDefault()?.Url,
            Id: userProfile.Id
        );
    }
}
```

Note: `AutoMapper` is no longer needed in `UserProfileService`. Remove the `IMapper` dependency.

#### 3. Refactor IPlaylistsService and PlaylistsService

**`SpotifyPlaylistSorter.Business/Services/IPlaylistsService.cs`:**
```csharp
using SpotifyPlaylistSorter.Business.Dtos;

namespace SpotifyPlaylistSorter.Business.Services;

public interface IPlaylistsService
{
    Task<PaginatedResultDto<PlaylistSummaryDto>?> GetPlaylists(int page, int pageSize);
    Task<PaginatedResultDto<TrackSummaryDto>?> GetPlaylistTracks(string playlistId, int page, int pageSize);
    Task<bool> AnalysePlaylist(string id);
}
```

**`SpotifyPlaylistSorter.Business/Services/PlaylistsService.cs`:**
Refactor to return DTOs instead of ViewModels. Remove `IMapper` dependency. Map SpotifyAPI.Web types to DTOs manually (similar to how `SpotifyService.GetTrackAsync` works):

```csharp
using System.Text.Json;
using SpotifyAPI.Web;
using SpotifyPlaylistSorter.Business.Dtos;
using SpotifyPlaylistSorter.Domain.Models.QueueMessages;

namespace SpotifyPlaylistSorter.Business.Services;

public class PlaylistsService : IPlaylistsService
{
    private readonly ISpotifyService _spotifyService;
    private readonly IMessageQueueService _messageQueueService;

    public PlaylistsService(
        ISpotifyService spotifyService,
        IMessageQueueService messageQueueService)
    {
        _spotifyService = spotifyService;
        _messageQueueService = messageQueueService;
    }

    public async Task<PaginatedResultDto<PlaylistSummaryDto>?> GetPlaylists(int page, int pageSize)
    {
        if (_spotifyService.SpotifyClient == null) return null;

        var request = new PlaylistCurrentUsersRequest
        {
            Limit = pageSize,
            Offset = (page - 1) * pageSize
        };

        var playlists = await _spotifyService.SpotifyClient.Playlists.CurrentUsers(request);
        if (playlists == null) return new PaginatedResultDto<PlaylistSummaryDto>();

        return new PaginatedResultDto<PlaylistSummaryDto>
        {
            Items = playlists.Items?.Select(p => new PlaylistSummaryDto
            {
                Name = p.Name,
                Description = p.Description,
                ImageUrl = p.Images?.FirstOrDefault()?.Url,
                TracksUrl = p.Tracks?.Href,
                TracksTotal = p.Tracks?.Total,
                SpotifyId = p.Id
            }).ToList() ?? new(),
            Total = playlists.Total ?? 0,
            Page = page,
            PageSize = pageSize,
            Next = playlists.Next,
            Previous = playlists.Previous
        };
    }

    public async Task<PaginatedResultDto<TrackSummaryDto>?> GetPlaylistTracks(string playlistId, int page, int pageSize)
    {
        if (_spotifyService.SpotifyClient == null) return null;

        var request = new PlaylistGetItemsRequest
        {
            Limit = pageSize,
            Offset = (page - 1) * pageSize,
        };

        var playlist = await _spotifyService.SpotifyClient.Playlists.GetItems(playlistId, request);
        if (playlist == null) return new PaginatedResultDto<TrackSummaryDto>();

        return new PaginatedResultDto<TrackSummaryDto>
        {
            Items = playlist.Items?.Select(t =>
            {
                var fullTrack = t.Track as FullTrack;
                return new TrackSummaryDto
                {
                    Name = fullTrack?.Name ?? string.Empty,
                    SpotifyTrackId = fullTrack?.Id ?? string.Empty,
                    Artists = fullTrack?.Artists?.Select(a => new ArtistDto
                    {
                        SpotifyArtistId = a.Id ?? string.Empty,
                        Name = a.Name
                    }).ToList() ?? new()
                };
            }).Where(t => !string.IsNullOrEmpty(t.SpotifyTrackId)).ToList() ?? new(),
            Total = playlist.Total ?? 0,
            Page = page,
            PageSize = pageSize,
            Next = playlist.Next,
            Previous = playlist.Previous
        };
    }

    public async Task<bool> AnalysePlaylist(string id)
    {
        // ... same implementation as current (GetAllTrackIds + QueueTracksForAnalysis)
    }

    // ... private methods GetAllTrackIds and QueueTracksForAnalysis unchanged
}
```

#### 4. Move ViewModels from Business to Web

Move these files from `SpotifyPlaylistSorter.Business/Models/` to `SpotifyPlaylistSorterWeb/Models/`:
- `SpotifyBaseViewModel.cs`
- `PlaylistsViewModel.cs`
- `FullPlaylistViewModel.cs`
- `CurrentUserViewModel.cs`
- `ErrorViewModel.cs`
- `PaginatedList.cs`
- `IPaginatedList.cs`
- `FullPlaylistModel.cs`
- `TrackModel.cs`

Update all namespaces from `SpotifyPlaylistSorter.Business.Models` → `SpotifyPlaylistSorterWeb.Models`

Note: `CurrentUserViewModel` no longer extends `PrivateUser`. Rewrite it as a standalone class:

```csharp
namespace SpotifyPlaylistSorterWeb.Models;

public class CurrentUserViewModel
{
    public bool IsLoggedIn { get; set; }
    public string? DisplayName { get; set; }
    public string? ImageUrl { get; set; }
    public string? Id { get; set; }
}
```

Note: `TrackModel` — remove the `FullTrack` constructor (it references SpotifyAPI.Web). Keep the default constructor and properties. The AutoMapper profile will handle mapping.

```csharp
using SpotifyPlaylistSorter.Business.Dtos;

namespace SpotifyPlaylistSorterWeb.Models;

public class TrackModel
{
    public required string Name { get; set; }
    public required List<ArtistDto> Artists { get; set; }
    public required string SpotifyTrackId { get; set; }
}
```

Update `PlaylistsViewModel` and `FullPlaylistViewModel` to use local types (they now reference `SpotifyPlaylistSorterWeb.Models.PaginatedList<>` etc. — same namespace, no changes needed beyond the namespace declaration).

#### 5. Update controllers

**`SpotifyPlaylistSorterWeb/Controllers/HomeController.cs`:**
```csharp
using Microsoft.AspNetCore.Mvc;
using SpotifyPlaylistSorter.Business.Services;
using SpotifyPlaylistSorterWeb.Models;

namespace SpotifyPlaylistSorterWeb.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly IUserProfileService _userProfileService;

    public HomeController(ILogger<HomeController> logger, IUserProfileService userProfileService)
    {
        _logger = logger;
        _userProfileService = userProfileService;
    }

    public async Task<IActionResult> Index()
    {
        var profile = await _userProfileService.GetCurrentUserProfile();
        var viewModel = new CurrentUserViewModel();
        if (profile != null)
        {
            viewModel.IsLoggedIn = true;
            viewModel.DisplayName = profile.DisplayName;
            viewModel.ImageUrl = profile.ImageUrl;
            viewModel.Id = profile.Id;
        }
        return View(viewModel);
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
```

**`SpotifyPlaylistSorterWeb/Controllers/PlaylistsController.cs`:**
```csharp
using Microsoft.AspNetCore.Mvc;
using SpotifyPlaylistSorter.Business.Services;
using SpotifyPlaylistSorterWeb.Models;

namespace SpotifyPlaylistSorterWeb.Controllers;

public class PlaylistsController : Controller
{
    private readonly ILogger<PlaylistsController> _logger;
    private readonly IPlaylistsService _playlistsService;

    public PlaylistsController(ILogger<PlaylistsController> logger, IPlaylistsService playlistsService)
    {
        _logger = logger;
        _playlistsService = playlistsService;
    }

    public async Task<IActionResult> Current(int page = 1, int pageSize = 10)
    {
        var result = await _playlistsService.GetPlaylists(page, pageSize);
        var viewModel = new PlaylistsViewModel(page, pageSize);
        if (result != null)
        {
            viewModel.IsLoggedIn = true;
            viewModel.Playlists.Total = result.Total;
            viewModel.Playlists.Next = result.Next;
            viewModel.Playlists.Previous = result.Previous;
            viewModel.Playlists.Items = result.Items.Select(p => new FullPlaylistModel
            {
                Name = p.Name,
                Description = p.Description,
                ImageUrl = p.ImageUrl,
                TracksUrl = p.TracksUrl,
                TracksTotal = p.TracksTotal,
                SpotifyId = p.SpotifyId
            }).ToList();
        }
        return View(viewModel);
    }

    public async Task<IActionResult> ViewPlaylist([FromRoute] string id, [FromQuery] int page = 1, [FromQuery] int pageSize = 100)
    {
        var result = await _playlistsService.GetPlaylistTracks(id, page, pageSize);
        var viewModel = new FullPlaylistViewModel(id, page, pageSize);
        if (result != null)
        {
            viewModel.IsLoggedIn = true;
            viewModel.Tracks.Total = result.Total;
            viewModel.Tracks.Next = result.Next;
            viewModel.Tracks.Previous = result.Previous;
            viewModel.Tracks.Items = result.Items.Select(t => new TrackModel
            {
                Name = t.Name,
                SpotifyTrackId = t.SpotifyTrackId,
                Artists = t.Artists
            }).ToList();
        }
        return View(viewModel);
    }
}
```

#### 6. Update MappingProfile

The `MappingProfile` in Web mapped SpotifyAPI.Web types to ViewModels. Since services now return DTOs and controllers build ViewModels manually, most AutoMapper mappings are no longer needed. The profile can be simplified or removed entirely.

If AutoMapper is no longer used by any service, remove:
- `AutoMapper` package from Business .csproj
- `AutoMapper.Extensions.Microsoft.DependencyInjection` package from Web .csproj
- `builder.Services.AddAutoMapper(typeof(MappingProfile));` from Web Program.cs
- `SpotifyPlaylistSorterWeb/Mappers/MappingProfile.cs`

#### 7. Delete old files from Business/Models

After moving ViewModels to Web, delete from `SpotifyPlaylistSorter.Business/Models/`:
- `SpotifyBaseViewModel.cs`
- `PlaylistsViewModel.cs`
- `FullPlaylistViewModel.cs`
- `CurrentUserViewModel.cs`
- `ErrorViewModel.cs`
- `PaginatedList.cs`
- `IPaginatedList.cs`
- `FullPlaylistModel.cs`
- `TrackModel.cs`

Keep: `SpotifyPlaylistSorter.Business/Models/Cyanite/` (response models for Cyanite API, part of `ICyaniteService` contract)

#### 8. Update Razor views

Update `@using` directives in views and `_ViewImports.cshtml`:
- Replace `@using SpotifyPlaylistSorter.Business.Models` → `@using SpotifyPlaylistSorterWeb.Models`

Check `_ViewImports.cshtml` for global using directives.

The `@model` directives in each view should continue to work since the class names haven't changed.

Also check if any views reference `CurrentUserViewModel` properties that came from `PrivateUser` (e.g., `Model.DisplayName`, `Model.Images`). Update to use the new simpler properties (`Model.DisplayName`, `Model.ImageUrl`).

### Success Criteria:

#### Automated Verification:
- [ ] Solution builds: `dotnet build`
- [ ] Tests pass: `dotnet test SpotifyPlaylistSorter.Business.Tests/`
- [ ] No ViewModel classes in `SpotifyPlaylistSorter.Business/Models/` (only Cyanite/ subdirectory remains)
- [ ] `grep -r "SpotifyPlaylistSorter.Business.Models" --include="*.cs"` only returns hits for Cyanite model references
- [ ] Business .csproj has no `AutoMapper` package reference

#### Manual Verification:
- [ ] Home page renders correctly (login status, user profile)
- [ ] Playlists page shows paginated playlists
- [ ] Playlist detail page shows tracks
- [ ] "Analyse" button works

---

## Phase 5: Final Cleanup — Project References, NuGet, DI Registration

### Overview
Clean up all project references, remove unnecessary NuGet packages, create Infrastructure DI extension method, and update both composition roots.

### Changes Required:

#### 1. Create DI extension method in Infrastructure

**`SpotifyPlaylistSorter.Infrastructure/DependencyInjection.cs`** (new):
```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SpotifyPlaylistSorter.Business.Clients.Interfaces;
using SpotifyPlaylistSorter.Business.Services;
using SpotifyPlaylistSorter.Domain.Repositories;
using SpotifyPlaylistSorter.Infrastructure.ExternalServices;
using SpotifyPlaylistSorter.Infrastructure.Messaging;
using SpotifyPlaylistSorter.Infrastructure.Persistence;

namespace SpotifyPlaylistSorter.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Database
        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));

        // Repositories
        services.AddTransient<ITrackStore, TrackStore>();

        // External services
        services.AddSingleton<ISpotifyService, SpotifyService>();
        services.AddHttpClient<ICyaniteClient, CyaniteClient>(client =>
        {
            client.BaseAddress = new System.Uri("https://api.cyanite.ai/graphql");
            client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue(
                    "Bearer", configuration.GetValue<string>("Cyanite:AccessToken"));
        });
        services.AddTransient<ICyaniteService, CyaniteService>();

        // Messaging
        services.AddScoped<IMessageQueueService, RabbitMQService>();

        return services;
    }
}
```

Note: `ISpotifyService` is registered as Singleton (matching existing behaviour). `ICyaniteClient` uses typed HttpClient. Some services are only used by Worker (e.g., `ICyaniteService`, `ITrackStore`) — registering them in both hosts is harmless since DI is lazy.

#### 2. Update Web Program.cs

**`SpotifyPlaylistSorterWeb/Program.cs`:**
```csharp
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SpotifyPlaylistSorter.Business.Services;
using SpotifyPlaylistSorter.Infrastructure;
using SpotifyPlaylistSorterWeb.Data;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString));

builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddDefaultIdentity<IdentityUser>(options => options.SignIn.RequireConfirmedAccount = true)
    .AddEntityFrameworkStores<ApplicationDbContext>();

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

builder.Services.AddDistributedMemoryCache();
builder.Services.AddControllersWithViews();

// Infrastructure (DB, external services, messaging)
builder.Services.AddInfrastructure(builder.Configuration);

// Business services
builder.Services.AddScoped<IUserProfileService, UserProfileService>();
builder.Services.AddScoped<IPlaylistsService, PlaylistsService>();

var app = builder.Build();
// ... rest unchanged
```

Note: `AddAutoMapper` line removed (AutoMapper no longer used). `ISpotifyService` and `IMessageQueueService` are now registered via `AddInfrastructure`. `UserProfileService` and `PlaylistsService` remain in Business and are registered here.

#### 3. Update Worker Program.cs

**`TrackAnalysisWorker/Program.cs`:**
```csharp
using TrackAnalysisWorker;
using SpotifyPlaylistSorter.Business.Services;
using SpotifyPlaylistSorter.Infrastructure;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddHostedService<Worker>();

// Infrastructure (DB, external services, messaging)
builder.Services.AddInfrastructure(builder.Configuration);

// Business services
builder.Services.AddTransient<IAnalyserService, PlaylistAnalyserService>();
builder.Services.AddHttpContextAccessor();

var host = builder.Build();
host.Run();
```

Note: `IHttpContextAccessor` is still needed because `SpotifyService` injects it (for session-based token storage in web context; no-ops in worker context).

#### 4. Clean up Web .csproj

**`SpotifyPlaylistSorterWeb/SpotifyPlaylistSorterWeb.csproj`:**

Remove:
```xml
<!-- Remove these -->
<PackageReference Include="AutoMapper.Extensions.Microsoft.DependencyInjection" Version="12.0.1" />
<PackageReference Include="RabbitMQ.Client" Version="7.1.2" />
<PackageReference Include="SpotifyAPI.Web" Version="7.2.1" />
<PackageReference Include="SpotifyAPI.Web.Auth" Version="7.2.1" />
```

Remove the Common project reference (already done in Phase 1). Ensure Infrastructure is referenced:
```xml
<ProjectReference Include="../SpotifyPlaylistSorter.Infrastructure/SpotifyPlaylistSorter.Infrastructure.csproj" />
```

Can also remove the Domain direct reference since it's transitively available through Infrastructure and Business:
```xml
<!-- Remove if not directly needed -->
<ProjectReference Include="../SpotifyPlaylistSorter.Domain/SpotifyPlaylistSorter.Domain.csproj" />
```

#### 5. Clean up Worker .csproj

**`TrackAnalysisWorker/TrackAnalysisWorker.csproj`:**

Remove (now provided by Infrastructure):
```xml
<!-- Remove these -->
<PackageReference Include="Microsoft.EntityFrameworkCore.Design" Version="9.0.4" />
<PackageReference Include="Npgsql.EntityFrameworkCore.PostgreSQL" Version="9.0.4" />
<PackageReference Include="RabbitMQ.Client" Version="7.1.2" />
```

Remove empty `<Folder>` entries. Remove Domain/Common direct references (transitive through Infrastructure/Business):
```xml
<!-- Remove these -->
<ProjectReference Include="../Common/Common.csproj" />
<ProjectReference Include="../SpotifyPlaylistSorter.Domain/SpotifyPlaylistSorter.Domain.csproj" />
```

Ensure Infrastructure is referenced:
```xml
<ProjectReference Include="../SpotifyPlaylistSorter.Infrastructure/SpotifyPlaylistSorter.Infrastructure.csproj" />
```

Keep: `Microsoft.Extensions.Hosting`, `Microsoft.Extensions.Http` (Worker needs these directly).

Note: Worker needs `Microsoft.EntityFrameworkCore.Design` if it's used as `--startup-project` for migrations. Either keep it in Worker or update the migration command to use a different startup project.

#### 6. Clean up Business .csproj (final)

**`SpotifyPlaylistSorter.Business/SpotifyPlaylistSorter.Business.csproj`:**
```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.Extensions.Logging.Abstractions" Version="9.0.9" />
    <PackageReference Include="SpotifyAPI.Web" Version="7.2.1" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="../SpotifyPlaylistSorter.Domain/SpotifyPlaylistSorter.Domain.csproj" />
  </ItemGroup>

</Project>
```

Note: `Microsoft.Extensions.Configuration` removed (no longer needed in Business). `AutoMapper` removed (ViewModel mapping moved to controllers). `SpotifyAPI.Web` kept (for `ISpotifyService` interface type).

#### 7. Clean up Tests .csproj

**`SpotifyPlaylistSorter.Business.Tests/SpotifyPlaylistSorter.Business.Tests.csproj`:**

Remove Common reference. Keep Business and Domain references:
```xml
<ItemGroup>
    <ProjectReference Include="..\SpotifyPlaylistSorter.Business\SpotifyPlaylistSorter.Business.csproj" />
    <ProjectReference Include="..\SpotifyPlaylistSorter.Domain\SpotifyPlaylistSorter.Domain.csproj" />
</ItemGroup>
```

Update any test files that reference old namespaces (e.g., `using SpotifyPlaylistSorter.Common.Models.QueueMessages` → `using SpotifyPlaylistSorter.Domain.Models.QueueMessages`, and `using SpotifyPlaylistSorter.Business.Services` for `ITrackStore` → `using SpotifyPlaylistSorter.Domain.Repositories`).

#### 8. Delete MappingProfile and Mappers directory from Web (if AutoMapper fully removed)

Delete:
- `SpotifyPlaylistSorterWeb/Mappers/MappingProfile.cs`
- `SpotifyPlaylistSorterWeb/Mappers/` directory

### Success Criteria:

#### Automated Verification:
- [ ] Solution builds: `dotnet build`
- [ ] Tests pass: `dotnet test SpotifyPlaylistSorter.Business.Tests/`
- [ ] No project references to Common: `grep -r "Common.csproj" --include="*.csproj" .` returns nothing
- [ ] Domain .csproj has zero PackageReference and zero ProjectReference entries
- [ ] Business .csproj has only `SpotifyAPI.Web` and `Microsoft.Extensions.Logging.Abstractions` packages
- [ ] No AutoMapper references anywhere: `grep -r "AutoMapper" --include="*.csproj" .` returns nothing

#### Manual Verification:
- [ ] Web app starts and login flow works
- [ ] Worker starts and processes queue messages
- [ ] Full analysis pipeline works end-to-end

---

## Phase 6: Verify Build and Tests

### Overview
Final verification that everything compiles and all tests pass.

### Steps:

1. `dotnet build` — verify zero errors, zero warnings related to our changes
2. `dotnet test SpotifyPlaylistSorter.Business.Tests/` — all 10 tests pass
3. Verify the final dependency graph:

```
SpotifyPlaylistSorterWeb
    ├── SpotifyPlaylistSorter.Infrastructure
    │   ├── SpotifyPlaylistSorter.Business
    │   │   └── SpotifyPlaylistSorter.Domain
    │   └── SpotifyPlaylistSorter.Domain
    └── SpotifyPlaylistSorter.Business
        └── SpotifyPlaylistSorter.Domain

TrackAnalysisWorker
    ├── SpotifyPlaylistSorter.Infrastructure
    │   ├── SpotifyPlaylistSorter.Business
    │   │   └── SpotifyPlaylistSorter.Domain
    │   └── SpotifyPlaylistSorter.Domain
    └── SpotifyPlaylistSorter.Business
        └── SpotifyPlaylistSorter.Domain

SpotifyPlaylistSorter.Business.Tests
    ├── SpotifyPlaylistSorter.Business
    │   └── SpotifyPlaylistSorter.Domain
    └── SpotifyPlaylistSorter.Domain
```

4. Verify no circular dependencies exist
5. Update CLAUDE.md if any commands or conventions changed

### Success Criteria:

#### Automated Verification:
- [ ] `dotnet build` succeeds
- [ ] `dotnet test SpotifyPlaylistSorter.Business.Tests/` — 10/10 pass
- [ ] Domain has no NuGet dependencies
- [ ] No references to `SpotifyPlaylistSorter.Common` namespace

#### Manual Verification:
- [ ] `dotnet run --project SpotifyPlaylistSorterWeb/` starts successfully
- [ ] `dotnet run --project TrackAnalysisWorker/` starts successfully

---

## Summary of File Movements

| From | To | Type |
|------|----|------|
| `Common/Models/AnalysePlaylist.cs` | `Domain/Models/QueueMessages/AnalysePlaylist.cs` | Move + rename namespace |
| `Common/Models/MessageBase.cs` | `Domain/Models/QueueMessages/MessageBase.cs` | Move + rename namespace |
| `Common/Enums/QueueMessageTypeEnum.cs` | `Domain/Models/QueueMessages/QueueMessageTypeEnum.cs` | Move + rename namespace |
| `Domain/AppDbContext.cs` | `Infrastructure/Persistence/AppDbContext.cs` | Move + rename namespace |
| `Domain/AppDbContextFactory.cs` | `Infrastructure/Persistence/AppDbContextFactory.cs` | Move + rename namespace |
| `Domain/Migrations/*` | `Infrastructure/Migrations/*` | Move + rename namespace |
| `Domain/Repositories/Repository.cs` | `Infrastructure/Persistence/Repository.cs` | Move + rename namespace |
| `Business/Services/ITrackStore.cs` | `Domain/Repositories/ITrackStore.cs` | Move + rename namespace |
| `Business/Services/TrackStore.cs` | `Infrastructure/Persistence/TrackStore.cs` | Move + rename namespace |
| `Business/Services/SpotifyService.cs` | `Infrastructure/ExternalServices/SpotifyService.cs` | Move + rename namespace |
| `Business/Services/CyaniteService.cs` | `Infrastructure/ExternalServices/CyaniteService.cs` | Move + rename namespace |
| `Business/Clients/Implementations/CyaniteClient.cs` | `Infrastructure/ExternalServices/CyaniteClient.cs` | Move + rename namespace |
| `Business/Services/RabbitMQService.cs` | `Infrastructure/Messaging/RabbitMQService.cs` | Move + rename namespace |
| `Business/Models/SpotifyBaseViewModel.cs` | `Web/Models/SpotifyBaseViewModel.cs` | Move + rename namespace |
| `Business/Models/PlaylistsViewModel.cs` | `Web/Models/PlaylistsViewModel.cs` | Move + rename namespace |
| `Business/Models/FullPlaylistViewModel.cs` | `Web/Models/FullPlaylistViewModel.cs` | Move + rename namespace |
| `Business/Models/CurrentUserViewModel.cs` | `Web/Models/CurrentUserViewModel.cs` | Rewrite (no longer extends PrivateUser) |
| `Business/Models/ErrorViewModel.cs` | `Web/Models/ErrorViewModel.cs` | Move + rename namespace |
| `Business/Models/PaginatedList.cs` | `Web/Models/PaginatedList.cs` | Move + rename namespace |
| `Business/Models/IPaginatedList.cs` | `Web/Models/IPaginatedList.cs` | Move + rename namespace |
| `Business/Models/FullPlaylistModel.cs` | `Web/Models/FullPlaylistModel.cs` | Move + rename namespace |
| `Business/Models/TrackModel.cs` | `Web/Models/TrackModel.cs` | Move + rewrite (remove FullTrack ctor) |

## New Files

| File | Purpose |
|------|---------|
| `Business/Dtos/UserProfileDto.cs` | DTO for user profile data |
| `Business/Dtos/PaginatedResultDto.cs` | Generic pagination DTO |
| `Business/Dtos/PlaylistSummaryDto.cs` | DTO for playlist browsing |
| `Business/Dtos/TrackSummaryDto.cs` | DTO for track listing |
| `Infrastructure/DependencyInjection.cs` | DI extension method |

## Deleted Files/Projects

| File | Reason |
|------|--------|
| `Common/` (entire project) | Absorbed into Domain |
| `Infrastructure/Class1.cs` | Empty placeholder |
| `SpotifyPlaylistSorterWeb/Mappers/MappingProfile.cs` | AutoMapper removed |

## References

- Research document: [2026-04-06-layer-functionality-research.md](2026-04-06-layer-functionality-research.md)
