using System.Text;
using System.Text.Json;
using SpotifyPlaylistSorter.Business.Clients.Interfaces;

namespace SpotifyPlaylistSorter.Infrastructure.ExternalServices;

public class CyaniteClient : ICyaniteClient
{
    private readonly HttpClient _httpClient;

    public CyaniteClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<string> GetAsync(string query, object? variables = null)
    {
        var requestBody = new
        {
            query,
            variables
        };

        var content = new StringContent(
            JsonSerializer.Serialize(requestBody),
            Encoding.UTF8,
            "application/json"
        );

        var response = await _httpClient.PostAsync("https://api.cyanite.ai/graphql", content);

        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            Console.WriteLine(errorContent);
            throw new HttpRequestException($"GraphQL query failed: {errorContent}");
        }

        var responseContent = await response.Content.ReadAsStringAsync();

        return responseContent;
    }
}
