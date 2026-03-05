using System.Text;
using System.Text.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using SpotifyPlaylistSorter.Business.Services;
using SpotifyPlaylistSorter.Common.Models.QueueMessages;

namespace TrackAnalysisWorker;

public class Worker : BackgroundService
{
    private IConnection _connection;
    private IChannel _channel;
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

            var factory = new ConnectionFactory() { HostName = "localhost" }; // Or your actual host
            _connection = await factory.CreateConnectionAsync();
            _channel = await _connection.CreateChannelAsync();

            await _channel.QueueDeclareAsync(queue: "message",
                                  durable: false,
                                  exclusive: false,
                                  autoDelete: false,
                                  arguments: null);

            var consumer = new AsyncEventingBasicConsumer(_channel);
            consumer.ReceivedAsync += async (sender, eventArgs) =>
            {
                byte[] body = eventArgs.Body.ToArray();
                var messageBody = Encoding.UTF8.GetString(body);
                var message = JsonSerializer.Deserialize<AnalysePlaylist>(messageBody);
                _logger.LogInformation(" [x] Received: {message}", message);

                await _analyserService.Analyse(message);

                await ((AsyncDefaultBasicConsumer)sender).Channel.BasicAckAsync(eventArgs.DeliveryTag, multiple: false);
            };

            var message = await _channel.BasicConsumeAsync(queue: "message",
                                  autoAck: false,
                                  consumer: consumer);

            Thread.Sleep(1000);
        }
    }
}
