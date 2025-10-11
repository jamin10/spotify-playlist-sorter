using SpotifyPlaylistSorter.Business.Models;

namespace SpotifyPlaylistSorter.Business.Clients.Interfaces;

public interface ICyaniteClient
{
    public Task<string> GetAsync(string query, object? variables = null);
}