using System.Text.Json;
using SpotifyPlaylistSorter.Business.Services;
using SpotifyPlaylistSorter.Common.Models.QueueMessages;

namespace TrackAnalysisWorker;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IMessageBroker _messageBroker;

    public Worker(ILogger<Worker> logger, IServiceScopeFactory scopeFactory, IMessageBroker messageBroker)
    {
        _logger = logger;
        _scopeFactory = scopeFactory;
        _messageBroker = messageBroker;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken) =>
        _messageBroker.SubscribeAsync(HandleMessageAsync, stoppingToken);

    private async Task HandleMessageAsync(string messageBody, CancellationToken cancellationToken)
    {
        var message = JsonSerializer.Deserialize<AnalysePlaylist>(messageBody);
        _logger.LogInformation(" [x] Received: {message}", message);

        using var scope = _scopeFactory.CreateScope();
        var analyserService = scope.ServiceProvider.GetRequiredService<IAnalyserService>();
        await analyserService.Analyse(message);
    }
}
