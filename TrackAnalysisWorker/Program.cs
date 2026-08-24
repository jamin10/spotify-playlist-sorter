using TrackAnalysisWorker;
using Microsoft.EntityFrameworkCore;
using SpotifyPlaylistSorter.Business.Clients.Interfaces;
using SpotifyPlaylistSorter.Business.Clients.Implementations;
using SpotifyPlaylistSorter.Business.Configuration;
using SpotifyPlaylistSorter.Business.Services;
using SpotifyPlaylistSorter.Domain;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddHostedService<Worker>();

builder.Services.Configure<RabbitMqOptions>(builder.Configuration.GetSection("RabbitMQ"));
builder.Services.AddSingleton<IMessageBroker, RabbitMqMessageBroker>();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddHttpClient<ICyaniteClient, CyaniteClient>(client =>
{
    client.BaseAddress = new Uri("https://api.cyanite.ai/graphql");
    client.DefaultRequestHeaders.Authorization =
        new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", builder.Configuration.GetValue<string>("Cyanite:AccessToken"));
});
builder.Services.AddTransient<ICyaniteService, CyaniteService>();
builder.Services.AddTransient<ITrackStore, TrackStore>();
builder.Services.AddTransient<IAnalyserService, PlaylistAnalyserService>();
builder.Services.AddSingleton<ISpotifyCredentialProvider, ClientCredentialsProvider>();
builder.Services.AddTransient<ISpotifyService, SpotifyService>();

var host = builder.Build();
host.Run();
