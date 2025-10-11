using SpotifyPlaylistSorter.Business.Models.Cyanite;

namespace SpotifyPlaylistSorter.Business.Services;

public interface ICyaniteService
{
    Task<CyaniteTrack> GetTrackAnalysis(string trackId);
}