using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SpotifyPlaylistSorter.Business.Clients.Interfaces;
using SpotifyPlaylistSorter.Business.Services;
using SpotifyPlaylistSorter.Domain.Repositories;
using SpotifyPlaylistSorter.Infrastructure.ExternalServices;
using SpotifyPlaylistSorter.Infrastructure.Messaging;
using SpotifyPlaylistSorter.Infrastructure.Persistence;

namespace SpotifyPlaylistSorter.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Database
        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));

        // Repositories
        services.AddTransient<ITrackStore, TrackStore>();

        // External services
        services.AddSingleton<ISpotifyService, SpotifyService>();
        services.AddHttpClient<ICyaniteClient, CyaniteClient>(client =>
        {
            client.BaseAddress = new System.Uri("https://api.cyanite.ai/graphql");
            client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue(
                    "Bearer", configuration.GetValue<string>("Cyanite:AccessToken"));
        });
        services.AddTransient<ICyaniteService, CyaniteService>();

        // Messaging
        services.AddScoped<IMessageQueueService, RabbitMQService>();

        return services;
    }
}
