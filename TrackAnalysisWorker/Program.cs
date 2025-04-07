using TrackAnalysisWorker;
using TrackAnalysisWorker.Clients.Implementations;
using TrackAnalysisWorker.Clients.Interfaces;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddHostedService<Worker>();

builder.Services.AddHttpClient<ICyaniteClient, CyaniteClient>(client =>
{
    client.BaseAddress = new Uri("https://api.cyanite.ai/graphql");
    client.DefaultRequestHeaders.Authorization = 
        new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", builder.Configuration.GetValue<string>("Cyanite:AccessToken"));
});

var host = builder.Build();
host.Run();
