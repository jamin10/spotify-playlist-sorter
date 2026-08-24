namespace SpotifyPlaylistSorter.Business.Configuration;

public class RabbitMqOptions
{
    public string HostName { get; set; } = "localhost";

    public string QueueName { get; set; } = "message";
}
