using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SpotifyAPI.Web;
using SpotifyPlaylistSorterWeb;
using SpotifyPlaylistSorterWeb.Clients.Implementations;
using SpotifyPlaylistSorterWeb.Clients.Interfaces;
using SpotifyPlaylistSorterWeb.Data;
using SpotifyPlaylistSorterWeb.Mappers;
using SpotifyPlaylistSorterWeb.Models;
using SpotifyPlaylistSorterWeb.Services;
using SpotifyPlaylistSorterWeb.Services.Interfaces;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddDefaultIdentity<IdentityUser>(options => options.SignIn.RequireConfirmedAccount = true)
    .AddEntityFrameworkStores<ApplicationDbContext>();
builder.Services.AddSession(options =>
    {
        options.IdleTimeout = TimeSpan.FromMinutes(30); // Set session timeout
        options.Cookie.HttpOnly = true;                 // Make the session cookie HTTP-only
        options.Cookie.IsEssential = true;              // Essential for GDPR compliance
    });

builder.Services.AddDistributedMemoryCache();
builder.Services.AddControllersWithViews();
builder.Services.AddSingleton<ISpotifyService, SpotifyService>();
builder.Services.AddScoped<IUserProfileService, UserProfileService>();
builder.Services.AddScoped<IPlaylistsService, PlaylistsService>();

builder.Services.AddHttpClient<ICyaniteClient, CyaniteClient>(client =>
{
    client.BaseAddress = new Uri("https://api.cyanite.ai/graphql");
    client.DefaultRequestHeaders.Authorization = 
        new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", builder.Configuration.GetValue<string>("Cyanite:AccessToken"));
});

builder.Services.AddAutoMapper(typeof(MappingProfile));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseSession();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
app.MapRazorPages();

app.Run();
