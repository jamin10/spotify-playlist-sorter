using SpotifyPlaylistSorterWeb.Models.QueueMessages;

namespace TrackAnalysisWorker.Services;

public interface IAnalyserService
{
    public Task<bool> Analyse(AnalysePlaylist id);
}