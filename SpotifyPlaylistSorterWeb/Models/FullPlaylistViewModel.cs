// namespace SpotifyPlaylistSorterWeb.Models;

// public class FullPlaylistViewModel : SpotifyBaseViewModel
// {
//     public FullPlaylistViewModel(string playlistId, int page, int pageSize)
//     {
//         Tracks = new PaginatedList<TrackModel>
//         {
//             Index = page,
//             Limit = pageSize,
//             Action = $"ViewPlaylist",
//             RouteValues = new Dictionary<string, string>{
//                 ["id"] = playlistId
//             }
//         };
//     }
//     public PaginatedList<TrackModel> Tracks { get; set; } = new PaginatedList<TrackModel>();
// }