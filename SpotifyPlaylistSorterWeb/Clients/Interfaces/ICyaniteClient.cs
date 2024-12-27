using SpotifyPlaylistSorterWeb.Models.Requests;

namespace SpotifyPlaylistSorterWeb.Clients.Interfaces;

public interface ICyaniteClient
{
    public Task<string> GetAsync(string query, object? variables = null);
}