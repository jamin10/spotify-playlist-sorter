using System.Text.Json.Serialization;

namespace SpotifyPlaylistSorter.Business.Models.Cyanite;

public class CyaniteTrack
{
    [JsonPropertyName("data")]
    public required Data Data { get; set;  }
}