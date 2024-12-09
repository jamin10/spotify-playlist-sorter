using AutoMapper;
using AutoMapper.Execution;
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
            .ForMember(dest => dest.TotalPlaylists, opt => opt.MapFrom(src => src.Total))
            .ForMember(dest => dest.Playlists, opt => opt.MapFrom(src => src.Items));

        CreateMap<FullPlaylist, PlaylistModel>(MemberList.Source)
            .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Name))
            .ForMember(dest => dest.TracksUrl, opt => opt.MapFrom(src => src.Href))
            .ForAllMembers(opts =>
            {
                opts.Condition((src, dest, srcMember) => srcMember != null);
            });

        CreateMap<Paging<PlaylistTrack<IPlayableItem>>, PlaylistModel>(MemberList.Source)
            .ForMember(dest => dest.TracksUrl, opt => opt.MapFrom(src => src.Href));
    }
}