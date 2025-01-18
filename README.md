# spotify-playlist-sorter
Web app to analyse Spotify playlists to get track metadata and characterisitics. Audio features can be used to sort and create new playlists on Spotify. 

Audio features include:
* BPM
* Key
* Genre
* Energy level

## Usage
Usage of this app requires connecting to the Spotify Developer API and the Cyanite API.

### Spotify Developer API 
1. Create an app in the [Spotify Developer portal](https://developer.spotify.com/dashboard)
2. Add a Redirect URI in the format `https://localhost:{PORT_NUMBER}/Spotify/Callback`
3. Create an `appsettings.Development.json` file in the root of `SpotifyPlaylistSorterWeb` and add the following credentials:
```
"SpotifyCredentials": {
    "ClientId": "{CLIENT_ID}",
    "ClientSecret": "{CLIENT_SECRET}",
    "RedirectUri": "{REDIRECT_URI}"
```
### Cyanite API
1. Register for a [Cyantie API](https://app.cyanite.ai/register) account
2. In the Cyanite.ai integration section click "Create new integration"
3. Generate a new secret and create the integration


