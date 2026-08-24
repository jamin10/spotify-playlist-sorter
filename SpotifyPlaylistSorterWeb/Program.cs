using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SpotifyPlaylistSorter.Business.Configuration;
using SpotifyPlaylistSorter.Business.Services;
using SpotifyPlaylistSorter.Domain;
using SpotifyPlaylistSorterWeb.Data;
using SpotifyPlaylistSorterWeb.Mappers;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString));

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

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

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ISession>(sp => sp.GetRequiredService<IHttpContextAccessor>().HttpContext!.Session);
builder.Services.AddScoped<SessionSpotifyCredentialProvider>();
builder.Services.AddScoped<ISpotifyCredentialProvider>(sp => sp.GetRequiredService<SessionSpotifyCredentialProvider>());
builder.Services.AddScoped<ISpotifySessionAuth>(sp => sp.GetRequiredService<SessionSpotifyCredentialProvider>());
builder.Services.AddScoped<ISpotifyUserContext, SpotifyUserContext>();
builder.Services.AddScoped<IUserProfileService, UserProfileService>();
builder.Services.AddScoped<IPlaylistsService, PlaylistsService>();

builder.Services.Configure<RabbitMqOptions>(builder.Configuration.GetSection("RabbitMQ"));
builder.Services.AddSingleton<IMessageBroker, RabbitMqMessageBroker>();

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
