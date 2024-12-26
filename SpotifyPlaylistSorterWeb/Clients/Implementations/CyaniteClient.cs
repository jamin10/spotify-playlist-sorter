using System.Text;
using System.Text.Json;
using SpotifyPlaylistSorterWeb.Clients.Interfaces;

namespace SpotifyPlaylistSorterWeb.Clients.Implementations;

public class CyaniteClient : ICyaniteClient
{
    private readonly HttpClient _httpClient;

    public CyaniteClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<string> GetAsync(string query)
    {

        var request = new
        {
            query
        };

        var content = new StringContent(
            JsonSerializer.Serialize(request),
            Encoding.UTF8,
            "application/json"
        );

        var response = await _httpClient.PostAsync("", content);

        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            throw new HttpRequestException($"GraphQL query failed: {errorContent}");
        }

        var responseContent = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<string>(responseContent, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });
        return result;
    }
}