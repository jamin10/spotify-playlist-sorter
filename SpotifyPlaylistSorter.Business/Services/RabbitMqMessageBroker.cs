using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using SpotifyPlaylistSorter.Business.Configuration;

namespace SpotifyPlaylistSorter.Business.Services;

public class RabbitMqMessageBroker : IMessageBroker, IAsyncDisposable
{
    private readonly RabbitMqOptions _options;
    private readonly ILogger<RabbitMqMessageBroker> _logger;
    private readonly SemaphoreSlim _connectionLock = new(1, 1);
    private IConnection? _connection;

    public RabbitMqMessageBroker(IOptions<RabbitMqOptions> options, ILogger<RabbitMqMessageBroker> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public async Task PublishAsync(string message, CancellationToken cancellationToken = default)
    {
        var connection = await GetConnectionAsync(cancellationToken);
        var channel = await connection.CreateChannelAsync(cancellationToken: cancellationToken);
        await using var _ = channel;

        await DeclareQueueAsync(channel, cancellationToken);

        var body = Encoding.UTF8.GetBytes(message);
        await channel.BasicPublishAsync(
            exchange: "",
            routingKey: _options.QueueName,
            mandatory: true,
            basicProperties: new BasicProperties { Persistent = true },
            body: body,
            cancellationToken: cancellationToken);
    }

    public async Task SubscribeAsync(Func<string, CancellationToken, Task> handler, CancellationToken stoppingToken)
    {
        var connection = await GetConnectionAsync(stoppingToken);
        var channel = await connection.CreateChannelAsync(cancellationToken: stoppingToken);
        await using var _ = channel;

        await DeclareQueueAsync(channel, stoppingToken);

        var consumer = new AsyncEventingBasicConsumer(channel);
        consumer.ReceivedAsync += async (_, eventArgs) =>
        {
            var messageBody = Encoding.UTF8.GetString(eventArgs.Body.ToArray());

            try
            {
                await handler(messageBody, stoppingToken);
                await channel.BasicAckAsync(eventArgs.DeliveryTag, multiple: false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to handle message, dropping without requeue: {Message}", messageBody);
                await channel.BasicNackAsync(eventArgs.DeliveryTag, multiple: false, requeue: false);
            }
        };

        await channel.BasicConsumeAsync(queue: _options.QueueName, autoAck: false, consumer: consumer, cancellationToken: stoppingToken);

        await AwaitCancellationAsync(stoppingToken);
    }

    private async Task<IConnection> GetConnectionAsync(CancellationToken cancellationToken)
    {
        if (_connection is { IsOpen: true })
        {
            return _connection;
        }

        await _connectionLock.WaitAsync(cancellationToken);
        try
        {
            if (_connection is { IsOpen: true })
            {
                return _connection;
            }

            var factory = new ConnectionFactory { HostName = _options.HostName };
            _connection = await factory.CreateConnectionAsync(cancellationToken);
            return _connection;
        }
        finally
        {
            _connectionLock.Release();
        }
    }

    private Task DeclareQueueAsync(IChannel channel, CancellationToken cancellationToken) =>
        channel.QueueDeclareAsync(
            queue: _options.QueueName,
            durable: false,
            exclusive: false,
            autoDelete: false,
            arguments: null,
            cancellationToken: cancellationToken);

    private static Task AwaitCancellationAsync(CancellationToken stoppingToken)
    {
        var tcs = new TaskCompletionSource();
        stoppingToken.Register(() => tcs.TrySetResult());
        return tcs.Task;
    }

    public async ValueTask DisposeAsync()
    {
        _connectionLock.Dispose();

        if (_connection is not null)
        {
            await _connection.DisposeAsync();
        }
    }
}
