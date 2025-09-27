using System.Threading.Tasks;
using RabbitMQ.Client;
using Microsoft.Extensions.Logging;

namespace SpotifyPlaylistSorter.Business.Services;

public class RabbitMQService : IMessageQueueService
{
    private readonly ILogger<RabbitMQService> _logger;

    public RabbitMQService(ILogger<RabbitMQService> logger)
    {
        _logger = logger;
    }

    public async Task SendMessage(string message)
    {
        var factory = new ConnectionFactory
        {
            HostName = "localhost",
        };

        using var connection = await factory.CreateConnectionAsync();
        using var channel = await connection.CreateChannelAsync();

        await channel.QueueDeclareAsync(
            queue: "message",
            durable: false,
            exclusive: false,
            autoDelete: false,
            arguments: null
        );

        var body = System.Text.Encoding.UTF8.GetBytes(message);

        await channel.BasicPublishAsync(
            exchange: "",
            routingKey: "message",
            mandatory: true,
            basicProperties: new BasicProperties { Persistent = true },
            body: body
        );

        Console.WriteLine($"Sent: {message}");
    }
}