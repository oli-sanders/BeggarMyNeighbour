using System.Net.Http.Json;
using System.Text.Json;
using CardGames.BeggarMyNeighbour.Scoreboard.Models;

namespace CardGames.BeggarMyNeighbour.Scoreboard.Web.Services;

/// <summary>
/// Reads the leaderboard from the Scoreboard API and supporting infrastructure.
/// </summary>
public class ScoreboardClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ScoreboardClient> _logger;
    private readonly IHttpClientFactory _httpClientFactory;

    public ScoreboardClient(HttpClient httpClient, ILogger<ScoreboardClient> logger, IHttpClientFactory httpClientFactory)
    {
        _httpClient = httpClient;
        _logger = logger;
        _httpClientFactory = httpClientFactory;
    }

    /// <summary>
    /// Fetch the top games from the scoreboard, highest number of moves first.
    /// Returns an empty list if the API cannot be reached.
    /// </summary>
    public async Task<IReadOnlyList<ScoreResponse>> GetTopScoresAsync(
        int page = 1, int pageSize = 100, int? players = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var query = $"api/scores?page={page}&pageSize={pageSize}";
            if (players.HasValue)
                query += $"&players={players.Value}";
            var scores = await _httpClient.GetFromJsonAsync<List<ScoreResponse>>(query, cancellationToken);
            return scores ?? new List<ScoreResponse>();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not load scores from the scoreboard API.");
            return new List<ScoreResponse>();
        }
    }

    /// <summary>
    /// Fetch the number of messages pending in the RabbitMQ verify queue.
    /// Returns null if the management API is unreachable or not configured.
    /// </summary>
    public async Task<int?> GetVerifyQueueDepthAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var client = _httpClientFactory.CreateClient("rabbitmq");
            var response = await client.GetAsync(
                "http://beggareventbus:15672/api/queues/%2F/verify_queue", cancellationToken);
            if (!response.IsSuccessStatusCode)
                return null;
            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.TryGetProperty("messages", out var messages))
                return messages.GetInt32();
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Could not fetch verify queue depth from RabbitMQ.");
            return null;
        }
    }
}
