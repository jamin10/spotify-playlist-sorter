using AutoMapper;
using SpotifyAPI.Web;
using SpotifyPlaylistSorterWeb.Models;

namespace SpotifyPlaylistSorterWeb.Mappers;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        CreateMap<PrivateUser, CurrentUserViewModel>();
    }
}