# Architecture review — candidate tracker

Output of an `/improve-codebase-architecture` pass (2026-08-23), scoped to the analysis
pipeline and the Web/Worker seam. Candidates use the vocabulary from the `codebase-design`
skill (module, interface, depth, seam, adapter, leverage, locality) — load that skill before
continuing any of these so the language stays consistent.

No `CONTEXT.md` or `docs/adr/` exist yet in this repo. If a future session settles on a name
for a new domain concept, or an ADR-worthy decision comes up (a candidate gets rejected for a
load-bearing reason), create those lazily per `docs/agents/domain.md`.

## Status

| # | Candidate | Strength | Status |
|---|-----------|----------|--------|
| 1 | Give the Spotify session its own seam | Strong | **Done** — [PR #5](https://github.com/jamin10/spotify-playlist-sorter/pull/5), merged to `develop` |
| 2 | Deepen the queue into one messaging module | Strong | **Done** — implemented, not yet committed |
| 3 | Give the analysis data a read seam | Worth exploring | Not started |
| 4 | Model Cyanite's "can't analyse this track" as data | Worth exploring | Not started |
| 5 | Retire the shadow seams left by the clean-architecture move | Strong (zero-risk) | Not started |

**Suggested next pick: Candidate 2** (Strong, and it's a live production correctness bug —
unbounded RabbitMQ connections plus duplicate message delivery). Candidate 5 is the cheapest
if a smaller session is wanted first (confirmed-dead code, no design discussion needed, just
delete and verify with a build).

To pick one up: tell Claude "let's explore candidate N" and go straight into the `grilling`
skill on it — the candidate write-ups below carry enough context to skip re-running the
`/improve-codebase-architecture` exploration pass.

---

## Candidate 2 — Deepen the queue into one messaging module

**Strength:** Strong · **Files:**
- `TrackAnalysisWorker/Worker.cs`
- `SpotifyPlaylistSorter.Business/Services/RabbitMQService.cs`
- `SpotifyPlaylistSorterWeb/Controllers/MessageQueueController.cs` (touched already by
  Candidate 1 — the dead `IMessageQueueService` field there is already removed)

**Problem:** `Worker.ExecuteAsync` opens a new RabbitMQ connection and registers a new
consumer every second, inside a `while` loop, never disposing the previous one — a
`Thread.Sleep(1000)` sits inside an `async` method with no reason for the outer loop to exist
at all (`BasicConsumeAsync` is event-driven; the correct shape is "connect once, then await
forever," not "poll and reconnect"). Left running, this means unbounded connection growth
against RabbitMQ and, worse, every queued message eventually gets delivered to N duplicate
consumers once more than one is live — so `IAnalyserService.Analyse` gets invoked multiple
times per message. Separately, `RabbitMQService.SendMessage` (used by the Web app to publish)
hand-rolls the exact same `ConnectionFactory`/`QueueDeclareAsync`/hardcoded `"message"` queue
name/`"localhost"` host a second time, in a different project — no shared concept of "what
queue do we talk to and how," and neither side reads the queue name/host from config.

**Solution:** One deep messaging module (e.g. `IMessageBroker`) owning connection lifecycle
and queue naming, with publish/subscribe as its entire interface. The worker's loop becomes
"connect once, then await the stopping token" — less code than the reconnect loop it replaces.

**Wins:** Locality (connection lifecycle owned once) · duplicate consumers stop stacking ·
queue name/host become config-driven, one place · worker shrinks from reconnect-loop to await.

**Open design questions for the grilling round** (not yet walked):
- Where does the shared module live — `Common` (already shared between Web/Worker) or
  `SpotifyPlaylistSorter.Business`?
- Does `IMessageBroker` need to be generic over message type, or is `string` (as today,
  pre-serialized JSON) good enough given `Common.Models.QueueMessages.AnalysePlaylist` is the
  only message shape in use?
- Retry/backoff behavior when RabbitMQ is unreachable at startup — in scope, or a separate
  future candidate?
- Docker/local-dev note: queue host is hardcoded to `"localhost"` today with no config
  fallback — confirm what the actual deployment target needs before hardcoding a config key
  shape.

---

## Candidate 3 — Give the analysis data a read seam

**Strength:** Worth exploring · **Files:**
- `SpotifyPlaylistSorter.Business/Services/ITrackStore.cs` (write-only today)
- `SpotifyPlaylistSorter.Business/Services/PlaylistsService.cs` (now depends on
  `ISpotifyUserContext` post-Candidate-1)
- `SpotifyPlaylistSorter.Domain/AppDbContext.cs`

**Problem:** `ITrackStore` is a deep module on the write side — five focused tests, no EF Core
in sight (see `SpotifyPlaylistSorter.Business.Tests/Services/PlaylistAnalyserServiceTests.cs`).
It has no read counterpart. `PlaylistsService` (Web) re-fetches straight from the Spotify SDK
every time via `ISpotifyUserContext`, so the audio-feature data the Worker persists to Postgres
is never read back by anything — the product's whole premise (sort playlists by
Cyanite-derived audio features) currently has no code path that displays or uses that data.

**Solution:** A symmetric read seam — a small interface (e.g. `ITrackReader` or
`IPlaylistQueryService`) returning tracks-with-features from Postgres — so the Playlists views
can sort/group by the analysis instead of bypassing it entirely.

**Wins:** Leverage (one read seam, N view models) · the write module finally has a caller ·
sorting-by-feature becomes possible.

**Open design questions for the grilling round:**
- Is this additive (a new page/view showing analysed tracks) or does it replace the existing
  live-Spotify-backed `Current`/`ViewPlaylist` views? The latter is a bigger, riskier change
  (pagination/AutoMapper shape covered by Candidate 1's `SpotifyUserContext` would need to
  either merge with or sit alongside DB-backed data).
- Does the Web app even have read access to Postgres today in a working sense? (`AppDbContext`
  is registered in `SpotifyPlaylistSorterWeb/Program.cs` but nothing currently injects it —
  confirm the connection string/migrations are actually reachable from Web, not just Worker,
  before designing the seam.)
- What "sort by audio feature" actually means product-wise (sort by energy? BPM range? both?)
  — this needs a product decision before an interface shape can be finalized.

---

## Candidate 4 — Model Cyanite's "can't analyse this track" as data

**Strength:** Worth exploring · **Files:**
- `SpotifyPlaylistSorter.Business/Clients/Implementations/CyaniteClient.cs`
- `SpotifyPlaylistSorter.Business/Models/Cyanite/SpotifyTrack.cs`
- `SpotifyPlaylistSorter.Business/Services/PlaylistAnalyserService.cs` (`FetchAnalysedTrackAsync`,
  ~line 60-73 as of the last review pass — re-check line numbers before editing)

**Problem:** The GraphQL query already asks Cyanite for a `SpotifyTrackError` union variant
(`... on SpotifyTrackError { message }`), but the C# model
(`SpotifyPlaylistSorter.Business/Models/Cyanite/SpotifyTrack.cs`) declares only the success
shape's fields as `required`, with no model at all for the error variant. If Cyanite returns
the error variant (track not analysed/not found), JSON deserialization of `CyaniteTrack`
throws because the required members are absent. That exception is caught three call-levels up
in `PlaylistAnalyserService` by a blanket `catch (Exception ex)` that also swallows genuine
transport failures, auth errors, and programming bugs, logging them all identically. The one
existing test for this failure path
(`PlaylistAnalyserServiceTests.Analyse_LogsWarning_WhenTrackAnalysisFails`) only exercises a
`SpotifyService` throw, not this Cyanite union-type path — so this specific failure mode is
untested today.

**Solution:** Deepen the Cyanite client's interface so "track not analysable" is a modeled
result the caller branches on locally, not an exception that happens to get caught upstream.

**Wins:** Locality (one failure mode, one branch) · `catch(Exception)` stops hiding real bugs ·
the interface says what can go wrong.

**Open design questions for the grilling round:**
- Result shape: a discriminated union type (e.g. `OneOf<AnalysedTrack, AnalysisFailure>`, no
  extra package currently in the solution for this — would need a lightweight hand-rolled
  type or a new dependency) vs. a nullable/out-param pattern vs. a dedicated exception type
  that's narrower than `Exception` (e.g. `TrackNotAnalysableException`) that
  `PlaylistAnalyserService` catches specifically. Given the existing codebase has no
  discriminated-union convention anywhere, a narrow custom exception may be the lower-friction
  choice here — worth asking the user which they'd rather introduce as a first instance of the
  pattern.
- Does `PlaylistAnalyserService`'s current per-track try/catch-and-log behavior stay the same
  once the failure is modeled (i.e. still skip the track and log a warning), or does modeling
  it as data change what the caller does with it (e.g. record *why* a track was skipped)?

---

## Candidate 5 — Retire the shadow seams left by the clean-architecture move

**Strength:** Strong (zero-risk — confirmed dead by grep, no design discussion needed) ·
**Files:**
- `SpotifyPlaylistSorter.Domain/Repositories/*` (duplicate `ITrackStore`, `IRepository<T>`/
  `Repository<T>` — the live `ITrackStore` is `SpotifyPlaylistSorter.Business/Services/ITrackStore.cs`)
- `SpotifyPlaylistSorter.Domain/Models/QueueMessages/*` (duplicate of
  `Common/Models/AnalysePlaylist.cs`, `MessageBase.cs`, `Common/Enums/QueueMessageTypeEnum.cs`)
- `SpotifyPlaylistSorterWeb/Models/*` (9 files, every line commented out — live versions are
  in `SpotifyPlaylistSorter.Business/Models/*`)
- `SpotifyPlaylistSorter.Business/Mappers/` (empty directory — the `TrackEntityMapper`
  CLAUDE.md describes was apparently never landed; confirm it's still true before deleting the
  directory, since Candidate 1's work didn't touch this)
- `Common/Class1.cs` (stub scaffold file from `dotnet new classlib`)

**Problem:** The Onionify/clean-architecture migration moved several seams to their new homes
but left the old copies in place. Understanding "where does this concept live" currently means
checking two locations and figuring out which one is dead.

**Solution:** Apply the deletion test literally — delete each one, confirm via `grep -r` that
nothing outside the file itself references it, then `dotnet build` to confirm.

**Wins:** Deletion test passes clean (nothing moves) · one `ITrackStore`, not two · grep-by-
filename tells the truth again.

**Notes for whoever picks this up:**
- Re-run the "nothing references this outside itself" grep before deleting anything — this
  list is from the 2026-08-23 review pass; if Candidates 2-4 land first, double check none of
  them accidentally started using one of these dead files.
- This is genuinely a "just do it" candidate — the grilling loop probably isn't needed beyond
  confirming the file list is still accurate; go straight to deleting + building + running
  `dotnet test SpotifyPlaylistSorter.Business.Tests/`.

---

## Context carried over from Candidate 1 (useful background for 2-5)

- `SpotifyAPI.Web` 7.2.1 is the SDK version in use (Business and Web both reference it
  explicitly; Worker gets it transitively). `ClientCredentialsAuthenticator` and
  `AuthorizationCodeAuthenticator` (both in this version) handle their own token refresh
  internally — lean on that rather than hand-rolling expiry tracking, same as Candidate 1 did.
- The test project (`SpotifyPlaylistSorter.Business.Tests`) is MSTest + Moq only, no ASP.NET
  Core test-hosting package. If a candidate needs to fake an ASP.NET Core type (like
  Candidate 1 faking `ISession`), write a minimal hand-rolled fake rather than adding a new
  test-hosting dependency — that's the pattern now established in
  `SessionSpotifyCredentialProviderTests.cs`.
- `dotnet build` at the repo root and `dotnet test SpotifyPlaylistSorter.Business.Tests/` both
  run without any external services (no Postgres/RabbitMQ needed) — use them as the
  fast-feedback loop while implementing; there's no way to smoke-test the running app itself
  in this environment.
