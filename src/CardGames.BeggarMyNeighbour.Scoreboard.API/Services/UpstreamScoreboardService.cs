using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using CardGames.BeggarMyNeighbour.Scoreboard.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace CardGames.BeggarMyNeighbour.Scoreboard.API.Services
{
    /// <summary>
    /// Optionally forwards qualifying scores to a parent (upstream) scoreboard.
    /// Enabled by setting <c>UpstreamScoreboard:Url</c> in configuration.
    /// Forwarding is fire-and-forget; failures are logged as warnings and do not
    /// affect the local scoreboard response.
    /// </summary>
    public class UpstreamScoreboardService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly string _upstreamUrl;
        private readonly ILogger<UpstreamScoreboardService> _logger;

        public UpstreamScoreboardService(
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration,
            ILogger<UpstreamScoreboardService> logger)
        {
            _httpClientFactory = httpClientFactory;
            _upstreamUrl = configuration["UpstreamScoreboard:Url"] ?? string.Empty;
            _logger = logger;

            if (IsConfigured)
                _logger.LogInformation("Upstream scoreboard configured: {Url}", _upstreamUrl);
        }

        public bool IsConfigured => !string.IsNullOrWhiteSpace(_upstreamUrl);

        /// <summary>
        /// Fire-and-forget: POST the score to the upstream scoreboard on a background thread.
        /// </summary>
        public void ForwardInBackground(ScoreRequest request)
        {
            if (!IsConfigured) return;
            _ = Task.Run(() => ForwardAsync(request));
        }

        private async Task ForwardAsync(ScoreRequest request)
        {
            try
            {
                var client = _httpClientFactory.CreateClient("upstream");
                var json = Newtonsoft.Json.JsonConvert.SerializeObject(request);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await client.PostAsync(_upstreamUrl, content);
                _logger.LogInformation(
                    "Forwarded score {Score} (user={User}, team={Team}) to upstream, HTTP {Status}",
                    request.Length, request.User, request.Team, (int)response.StatusCode);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not forward score to upstream scoreboard at {Url}", _upstreamUrl);
            }
        }
    }
}
