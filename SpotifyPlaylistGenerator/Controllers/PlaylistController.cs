using Microsoft.AspNetCore.Mvc;
using SpotifyAPI.Web;

namespace SpotifyPlaylistGenerator.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PlaylistController : ControllerBase
    {
        private readonly IConfiguration _config;
        private readonly ILogger<PlaylistController> _logger;

        // Inject IConfiguration to access appsettings.json and ILogger for logging
        public PlaylistController(IConfiguration config, ILogger<PlaylistController> logger)
        {
            _config = config;
            _logger = logger;
        }

        [HttpGet("generate")]
        public async Task<IActionResult> GeneratePlaylist(string mood)
        {
            // Log the incoming request
            _logger.LogInformation("Received request to generate playlist for mood: {Mood}", mood);

            // Fetch Spotify API credentials from appsettings.json
            var clientId = _config.GetValue<string>("Spotify:ClientId");
            var clientSecret = _config.GetValue<string>("Spotify:ClientSecret");

            // Log the credentials loading status
            if (string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(clientSecret))
            {
                _logger.LogError("Spotify API credentials are missing in appsettings.json.");
                return StatusCode(500, new { message = "Spotify API credentials are not configured properly." });
            }

            try
            {
                // Authenticate with Spotify
                _logger.LogInformation("Authenticating with Spotify...");
                var config = SpotifyClientConfig.CreateDefault();
                var request = new ClientCredentialsRequest(clientId, clientSecret);
                var response = await new OAuthClient(config).RequestToken(request);

                var spotify = new SpotifyClient(config.WithToken(response.AccessToken));
                _logger.LogInformation("Successfully authenticated with Spotify.");

                // Search Spotify for playlists based on the provided mood
                _logger.LogInformation("Searching Spotify for playlists matching mood: {Mood}", mood);
                var random = new Random();
                var searchResults = await spotify.Search.Item(new SearchRequest(SearchRequest.Types.Playlist, mood));
                var playlist = searchResults.Playlists?.Items  
                    .OrderBy(x => random.Next())
                    .FirstOrDefault();
                

                if (playlist == null)
                {
                    _logger.LogWarning("No playlist found for mood: {Mood}", mood);
                    return NotFound(new { message = $"No playlist found for mood '{mood}'." });
                }

                // Log the playlist details
                _logger.LogInformation("Found playlist: {Name}, URL: {Url}", playlist.Name, playlist.ExternalUrls["spotify"]);

                // Return playlist details
                return Ok(new
                {
                    Name = playlist.Name,
                    Description = playlist.Description,
                    SpotifyUrl = playlist.ExternalUrls.ContainsKey("spotify") ? playlist.ExternalUrls["spotify"] : null
                });
            }
            catch (Exception ex)
            {
                // Log the exception
                _logger.LogError(ex, "An error occurred while generating the playlist.");
                return StatusCode(500, new { message = "An error occurred while generating the playlist." });
            }
        }
    }
}
