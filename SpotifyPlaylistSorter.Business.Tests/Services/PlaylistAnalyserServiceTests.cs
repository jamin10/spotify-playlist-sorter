using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SpotifyPlaylistSorter.Business.Dtos;
using SpotifyPlaylistSorter.Business.Models.Cyanite;
using SpotifyPlaylistSorter.Business.Services;
using SpotifyPlaylistSorter.Common.Models.QueueMessages;
using DomainModels = SpotifyPlaylistSorter.Domain.Models;

namespace SpotifyPlaylistSorter.Business.Tests.Services;

[TestClass]
public class PlaylistAnalyserServiceTests
{
    private Mock<ICyaniteService> _cyanite = null!;
    private Mock<ISpotifyService> _spotify = null!;
    private Mock<ITrackStore> _store = null!;
    private Mock<ILogger<PlaylistAnalyserService>> _logger = null!;
    private PlaylistAnalyserService _sut = null!;

    [TestInitialize]
    public void Setup()
    {
        _cyanite = new Mock<ICyaniteService>();
        _spotify = new Mock<ISpotifyService>();
        _store = new Mock<ITrackStore>();
        _logger = new Mock<ILogger<PlaylistAnalyserService>>();
        _sut = new PlaylistAnalyserService(_cyanite.Object, _spotify.Object, _store.Object, _logger.Object);

        // Default: empty DB, no playlist
        _store.Setup(s => s.GetExistingTracksAsync(It.IsAny<IEnumerable<string>>()))
              .ReturnsAsync([]);
        _store.Setup(s => s.FindPlaylistAsync(It.IsAny<string>()))
              .ReturnsAsync((DomainModels.Playlist?)null);
        _store.Setup(s => s.FindArtistAsync(It.IsAny<string>()))
              .ReturnsAsync((DomainModels.Artist?)null);
        _store.Setup(s => s.FindAlbumAsync(It.IsAny<string>()))
              .ReturnsAsync((DomainModels.Album?)null);
        _store.Setup(s => s.SaveChangesAsync()).Returns(Task.CompletedTask);

        _spotify.Setup(s => s.GetPlaylistAsync(It.IsAny<string>()))
                .ReturnsAsync(new SpotifyPlaylistDto("playlist1", "My Playlist", null, null));
    }

    [TestMethod]
    public async Task Analyse_SkipsExternalCalls_ForTracksAlreadyInStore()
    {
        var trackId = "existing-track";
        var existingTrack = new DomainModels.Track
        {
            SpotifyTrackId = trackId,
            Playlists = []
        };
        _store.Setup(s => s.GetExistingTracksAsync(It.IsAny<IEnumerable<string>>()))
              .ReturnsAsync([existingTrack]);

        await _sut.Analyse(new AnalysePlaylist("playlist1", [trackId]));

        _spotify.Verify(s => s.GetTrackAsync(It.IsAny<string>()), Times.Never);
        _cyanite.Verify(s => s.GetTrackAnalysis(It.IsAny<string>()), Times.Never);
    }

    [TestMethod]
    public async Task Analyse_CreatesNewPlaylist_WhenNotFoundInStore()
    {
        _spotify.Setup(s => s.GetTrackAsync(It.IsAny<string>()))
                .ReturnsAsync(MakeSpotifyTrackDto("track1"));
        _cyanite.Setup(s => s.GetTrackAnalysis(It.IsAny<string>()))
                .ReturnsAsync(MakeCyaniteTrack());

        DomainModels.Playlist? addedPlaylist = null;
        _store.Setup(s => s.Add(It.IsAny<DomainModels.Playlist>()))
              .Callback<DomainModels.Playlist>(p => addedPlaylist = p);

        await _sut.Analyse(new AnalysePlaylist("playlist1", ["track1"]));

        Assert.IsNotNull(addedPlaylist);
        Assert.AreEqual("playlist1", addedPlaylist.SpotifyPlaylistId);
        Assert.AreEqual("My Playlist", addedPlaylist.Name);
    }

    [TestMethod]
    public async Task Analyse_UsesExistingPlaylist_WhenFoundInStore()
    {
        var existingPlaylist = new DomainModels.Playlist { SpotifyPlaylistId = "playlist1", Name = "Old Name" };
        _store.Setup(s => s.FindPlaylistAsync("playlist1")).ReturnsAsync(existingPlaylist);

        await _sut.Analyse(new AnalysePlaylist("playlist1", []));

        _spotify.Verify(s => s.GetPlaylistAsync(It.IsAny<string>()), Times.Never);
    }

    [TestMethod]
    public async Task Analyse_LogsWarning_WhenTrackAnalysisFails()
    {
        _spotify.Setup(s => s.GetTrackAsync("bad-track"))
                .ThrowsAsync(new HttpRequestException("Spotify error"));

        await _sut.Analyse(new AnalysePlaylist("playlist1", ["bad-track"]));

        _logger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => true),
                It.IsAny<Exception>(),
                It.Is<Func<It.IsAnyType, Exception?, string>>((v, t) => true)),
            Times.Once);
    }

    [TestMethod]
    public async Task Analyse_AssociatesAllTracks_WithPlaylist()
    {
        var existingTrack = new DomainModels.Track { SpotifyTrackId = "existing", Playlists = [] };
        _store.Setup(s => s.GetExistingTracksAsync(It.IsAny<IEnumerable<string>>()))
              .ReturnsAsync([existingTrack]);

        _spotify.Setup(s => s.GetTrackAsync("new-track"))
                .ReturnsAsync(MakeSpotifyTrackDto("new-track"));
        _cyanite.Setup(s => s.GetTrackAnalysis("new-track"))
                .ReturnsAsync(MakeCyaniteTrack());

        DomainModels.Playlist? createdPlaylist = null;
        _store.Setup(s => s.Add(It.IsAny<DomainModels.Playlist>()))
              .Callback<DomainModels.Playlist>(p => createdPlaylist = p);

        await _sut.Analyse(new AnalysePlaylist("playlist1", ["existing", "new-track"]));

        Assert.IsNotNull(createdPlaylist);
        Assert.IsTrue(createdPlaylist!.Tracks.Contains(existingTrack),
            "Existing track should be linked to the playlist");
    }

    private static SpotifyTrackDto MakeSpotifyTrackDto(string trackId) => new(
        SpotifyTrackId: trackId,
        Title: "Test Track",
        Album: new AlbumModelDto
        {
            SpotifyId = "album1",
            Name = "Test Album",
            Artists = [new ArtistDto { SpotifyArtistId = "artist1", Name = "Test Artist" }]
        },
        Artists: [new ArtistDto { SpotifyArtistId = "artist1", Name = "Test Artist" }]
    );

    private static CyaniteTrack MakeCyaniteTrack() => new()
    {
        Data = new Data
        {
            SpotifyTrack = new SpotifyPlaylistSorter.Business.Models.Cyanite.SpotifyTrack
            {
                __typename = "SpotifyTrackResult",
                Id = "track1",
                Title = "Test Track",
                AudioAnalysisV6 = new AudioAnalysisV6
                {
                    __typename = "AudioAnalysisV6Finished",
                    Result = new AudioAnalysisResult
                    {
                        EnergyLevel = "HIGH",
                        EnergyDynamics = "CONSTANT",
                        BpmPrediction = new BpmPrediction { Value = 120.0, Confidence = 0.9 },
                        BpmRangeAdjusted = 120
                    }
                }
            }
        }
    };
}
