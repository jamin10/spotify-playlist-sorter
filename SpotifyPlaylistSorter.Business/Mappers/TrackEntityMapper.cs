using SpotifyPlaylistSorter.Business.Dtos;
using DomainModels = SpotifyPlaylistSorter.Domain.Models;

namespace SpotifyPlaylistSorter.Business.Mappers;

public static class TrackEntityMapper
{
    public static DomainModels.Track ToTrack(
        AnalysedTrackDto dto,
        DomainModels.Album album,
        List<DomainModels.Artist> artists)
        => new()
        {
            SpotifyTrackId = dto.SpotifyTrackId,
            Title = dto.Title,
            Album = album,
            Artists = artists,
            AudioFeatures = new DomainModels.TrackAudioFeatures
            {
                EnergyLevel = dto.AudioFeatures.EnergyLevel,
                EnergyDynamics = dto.AudioFeatures.EnergyDynamics,
                Bpm = dto.AudioFeatures.Bpm,
                BpmRangeAdjusted = dto.AudioFeatures.BpmRangeAdjusted
            }
        };
}
