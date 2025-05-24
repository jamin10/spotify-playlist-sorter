using TrackAnalysisWorker.Services;

namespace TrackAnalysisWorker;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly IAnalyserService _analyserService;

    public Worker(ILogger<Worker> logger, IAnalyserService analyserService)
    {
        _logger = logger;
        _analyserService = analyserService;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            if (_logger.IsEnabled(LogLevel.Information))
            {
                _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
            }

            _analyserService.Analyse("id");

            await Task.Delay(1000, stoppingToken);
        }
    }
}
