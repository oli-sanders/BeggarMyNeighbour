using System.Net.Http.Json;
using CardGames.BeggarMyNeighbour.Scoreboard.Models;

namespace CardGames.BeggarMyNeighbour.Scoreboard.Web.Services;

/// <summary>
/// Reads the leaderboard from the Scoreboard API.
/// </summary>
public class ScoreboardClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ScoreboardClient> _logger;

    public ScoreboardClient(HttpClient httpClient, ILogger<ScoreboardClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    /// <summary>
    /// Fetch the top games from the scoreboard, highest number of moves first.
    /// Returns an empty list if the API cannot be reached.
    /// </summary>
    public async Task<IReadOnlyList<ScoreResponse>> GetTopScoresAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var scores = await _httpClient.GetFromJsonAsync<List<ScoreResponse>>("api/scores", cancellationToken);
            return scores ?? new List<ScoreResponse>();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not load scores from the scoreboard API.");
            return new List<ScoreResponse>();
        }
    }
}
