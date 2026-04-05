using Microsoft.VisualStudio.TestTools.UnitTesting;
using SpotifyPlaylistSorter.Business.Dtos;
using SpotifyPlaylistSorter.Business.Mappers;
using DomainModels = SpotifyPlaylistSorter.Domain.Models;

namespace SpotifyPlaylistSorter.Business.Tests.Mappers;

[TestClass]
public class TrackEntityMapperTests
{
    [TestMethod]
    public void ToTrack_MapsSpotifyTrackId()
    {
        var dto = MakeDto("spotify-track-99");
        var track = TrackEntityMapper.ToTrack(dto, new DomainModels.Album(), []);
        Assert.AreEqual("spotify-track-99", track.SpotifyTrackId);
    }

    [TestMethod]
    public void ToTrack_MapsTitle()
    {
        var dto = MakeDto(title: "My Song");
        var track = TrackEntityMapper.ToTrack(dto, new DomainModels.Album(), []);
        Assert.AreEqual("My Song", track.Title);
    }

    [TestMethod]
    public void ToTrack_AssignsAlbum()
    {
        var album = new DomainModels.Album { SpotifyId = "alb1", Name = "Album Name" };
        var track = TrackEntityMapper.ToTrack(MakeDto(), album, []);
        Assert.AreSame(album, track.Album);
    }

    [TestMethod]
    public void ToTrack_AssignsArtists()
    {
        var artists = new List<DomainModels.Artist>
        {
            new() { SpotifyArtistId = "a1", Name = "Artist One" }
        };
        var track = TrackEntityMapper.ToTrack(MakeDto(), new DomainModels.Album(), artists);
        CollectionAssert.AreEqual(artists, track.Artists.ToList());
    }

    [TestMethod]
    public void ToTrack_MapsAudioFeatures()
    {
        var dto = MakeDto();
        dto.AudioFeatures.EnergyLevel = "HIGH";
        dto.AudioFeatures.EnergyDynamics = "CONSTANT";
        dto.AudioFeatures.Bpm = 128;
        dto.AudioFeatures.BpmRangeAdjusted = 130;

        var track = TrackEntityMapper.ToTrack(dto, new DomainModels.Album(), []);

        Assert.IsNotNull(track.AudioFeatures);
        Assert.AreEqual("HIGH", track.AudioFeatures.EnergyLevel);
        Assert.AreEqual("CONSTANT", track.AudioFeatures.EnergyDynamics);
        Assert.AreEqual(128, track.AudioFeatures.Bpm);
        Assert.AreEqual(130, track.AudioFeatures.BpmRangeAdjusted);
    }

    private static AnalysedTrackDto MakeDto(string trackId = "track1", string title = "Track Title")
    {
        var dto = new AnalysedTrackDto(
            new SpotifyTrackDto(
                SpotifyTrackId: trackId,
                Title: title,
                Album: new AlbumModelDto { SpotifyId = "alb1", Name = "Album", Artists = [] },
                Artists: []
            ),
            new SpotifyPlaylistSorter.Business.Models.Cyanite.CyaniteTrack
            {
                Data = new SpotifyPlaylistSorter.Business.Models.Cyanite.Data
                {
                    SpotifyTrack = new SpotifyPlaylistSorter.Business.Models.Cyanite.SpotifyTrack
                    {
                        __typename = "SpotifyTrackResult",
                        Id = trackId,
                        Title = title,
                        AudioAnalysisV6 = new SpotifyPlaylistSorter.Business.Models.Cyanite.AudioAnalysisV6
                        {
                            __typename = "AudioAnalysisV6Finished",
                            Result = null
                        }
                    }
                }
            }
        );
        return dto;
    }
}
