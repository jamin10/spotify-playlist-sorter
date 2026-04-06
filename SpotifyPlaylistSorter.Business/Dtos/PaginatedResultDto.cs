namespace SpotifyPlaylistSorter.Business.Dtos;

public class PaginatedResultDto<T>
{
    public List<T> Items { get; set; } = new();
    public int Total { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public string? Next { get; set; }
    public string? Previous { get; set; }
}
