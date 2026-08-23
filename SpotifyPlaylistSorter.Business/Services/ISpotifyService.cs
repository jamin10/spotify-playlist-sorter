using SpotifyPlaylistSorter.Business.Dtos;

namespace SpotifyPlaylistSorter.Business.Services;

/// <summary>
/// Public-data catalog lookups, used only by the Worker's analysis pipeline.
/// </summary>
public interface ISpotifyService
{
    Task<SpotifyTrackDto> GetTrackAsync(string trackId);

    Task<SpotifyPlaylistDto> GetPlaylistAsync(string playlistId);
}
