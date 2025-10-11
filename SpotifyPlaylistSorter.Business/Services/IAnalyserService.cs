using SpotifyPlaylistSorter.Common.Models.QueueMessages;

namespace SpotifyPlaylistSorter.Business.Services;

public interface IAnalyserService
{
    public Task<bool> Analyse(AnalysePlaylist id);
}