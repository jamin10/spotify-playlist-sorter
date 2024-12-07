using AutoMapper;
using SpotifyAPI.Web;
using SpotifyPlaylistSorterWeb.Models;

namespace SpotifyPlaylistSorterWeb.Mappers;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        CreateMap<PrivateUser, CurrentUserViewModel>(MemberList.Source);

        CreateMap<Paging<FullPlaylist>, PlaylistsViewModel>(MemberList.Source)
            .ForMember(dest => dest.NextPage, opt => opt.MapFrom(src => src.Next))
            .ForMember(dest => dest.PrevPage, opt => opt.MapFrom(src => src.Previous))
            .ForMember(dest => dest.TotalPlaylists, opt => opt.MapFrom(src => src.Total));
;
    }
}