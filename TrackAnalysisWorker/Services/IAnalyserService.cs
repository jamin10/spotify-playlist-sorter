namespace TrackAnalysisWorker.Services;

public interface IAnalyserService
{
    public Task<bool> Analyse(string id);
}