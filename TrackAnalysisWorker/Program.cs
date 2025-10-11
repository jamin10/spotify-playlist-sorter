using TrackAnalysisWorker;
using SpotifyPlaylistSorter.Business.Clients.Interfaces;
using SpotifyPlaylistSorter.Business.Clients.Implementations;
using SpotifyPlaylistSorter.Business.Services;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddHostedService<Worker>();

builder.Services.AddHttpClient<ICyaniteClient, CyaniteClient>(client =>
{
    client.BaseAddress = new Uri("https://api.cyanite.ai/graphql");
    client.DefaultRequestHeaders.Authorization =
        new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", builder.Configuration.GetValue<string>("Cyanite:AccessToken"));
});
builder.Services.AddTransient<IAnalyserService, PlaylistAnalyserService>();
builder.Services.AddSingleton<ISpotifyService, SpotifyService>();
builder.Services.AddHttpContextAccessor();

var host = builder.Build();
host.Run();
