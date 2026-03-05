using System.Text.Json.Serialization;

namespace SpotifyPlaylistSorter.Business.Models.Cyanite;

public class Data
{
    [JsonPropertyName("spotifyTrack")]
    public required SpotifyTrack SpotifyTrack { get; set; }
}