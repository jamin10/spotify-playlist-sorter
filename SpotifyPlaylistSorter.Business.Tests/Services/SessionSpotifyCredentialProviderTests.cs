using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using SpotifyPlaylistSorter.Business.Services;

namespace SpotifyPlaylistSorter.Business.Tests.Services;

[TestClass]
public class SessionSpotifyCredentialProviderTests
{
    private static IConfiguration MakeConfiguration() =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["SpotifyCredentials:ClientId"] = "client-id",
                ["SpotifyCredentials:ClientSecret"] = "client-secret",
                ["SpotifyCredentials:RedirectUri"] = "https://localhost/callback"
            })
            .Build();

    [TestMethod]
    public void IsAuthenticated_IsFalse_WhenSessionHasNoToken()
    {
        var sut = new SessionSpotifyCredentialProvider(new FakeSession(), MakeConfiguration());

        Assert.IsFalse(sut.IsAuthenticated);
    }

    [TestMethod]
    public void IsAuthenticated_IsTrue_AfterATokenIsWrittenToSession()
    {
        var session = new FakeSession();
        WriteFakeToken(session);

        var sut = new SessionSpotifyCredentialProvider(session, MakeConfiguration());

        Assert.IsTrue(sut.IsAuthenticated);
    }

    [TestMethod]
    public async Task GetClientAsync_Throws_WhenNoSessionExists()
    {
        var sut = new SessionSpotifyCredentialProvider(new FakeSession(), MakeConfiguration());

        await Assert.ThrowsExceptionAsync<InvalidOperationException>(() => sut.GetClientAsync());
    }

    [TestMethod]
    public void SignOut_RemovesTheStoredToken()
    {
        var session = new FakeSession();
        WriteFakeToken(session);
        var sut = new SessionSpotifyCredentialProvider(session, MakeConfiguration());
        Assert.IsTrue(sut.IsAuthenticated);

        sut.SignOut();

        Assert.IsFalse(sut.IsAuthenticated);
    }

    private static void WriteFakeToken(ISession session)
    {
        var json = """{"AccessToken":"access","RefreshToken":"refresh","TokenType":"Bearer","ExpiresIn":3600,"Scope":"playlist-read-private","CreatedAt":"2026-01-01T00:00:00Z"}""";
        session.Set("spotify_token", System.Text.Encoding.UTF8.GetBytes(json));
    }

    private class FakeSession : ISession
    {
        private readonly Dictionary<string, byte[]> _store = new();

        public bool IsAvailable => true;
        public string Id => "fake-session";
        public IEnumerable<string> Keys => _store.Keys;

        public Task LoadAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task CommitAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

        public bool TryGetValue(string key, out byte[] value) => _store.TryGetValue(key, out value!);
        public void Set(string key, byte[] value) => _store[key] = value;
        public void Remove(string key) => _store.Remove(key);
        public void Clear() => _store.Clear();
    }
}
