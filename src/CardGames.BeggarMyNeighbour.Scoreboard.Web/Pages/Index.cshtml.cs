using System.Text.Json;
using CardGames.BeggarMyNeighbour.Scoreboard.Models;
using CardGames.BeggarMyNeighbour.Scoreboard.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CardGames.BeggarMyNeighbour.Scoreboard.Web.Pages;

public class IndexModel : PageModel
{
    private readonly ScoreboardClient _scoreboard;

    public IndexModel(ScoreboardClient scoreboard)
    {
        _scoreboard = scoreboard;
    }

    /// <summary>Optional filter: only show games found by this user.</summary>
    [BindProperty(SupportsGet = true, Name = "user")]
    public string? UserName { get; set; }

    /// <summary>Optional filter: only show games found by this strategy.</summary>
    [BindProperty(SupportsGet = true)]
    public string? Strategy { get; set; }

    /// <summary>Optional filter: only show games for this number of players.</summary>
    [BindProperty(SupportsGet = true)]
    public int? Players { get; set; }

    public IReadOnlyList<ScoreResponse> Scores { get; private set; } = new List<ScoreResponse>();

    public IReadOnlyList<string> Strategies { get; private set; } = new List<string>();

    public IReadOnlyList<int> PlayerCounts { get; private set; } = new List<int>();

    /// <summary>
    /// JSON array of {x, y, strategy} chart points drawn from the full (unfiltered) scoreboard.
    /// x = ISO 8601 submitted timestamp, y = move count.
    /// </summary>
    public string ChartDataJson { get; private set; } = "[]";

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        var all = await _scoreboard.GetTopScoresAsync(cancellationToken);

        // Build the filter option lists from the full result set.
        Strategies = all.Select(s => s.Strategy)
            .Where(s => !string.IsNullOrEmpty(s))
            .Distinct()
            .OrderBy(s => s)
            .ToList()!;

        PlayerCounts = all.Select(s => s.Players).Distinct().OrderBy(p => p).ToList();

        IEnumerable<ScoreResponse> filtered = all;

        if (!string.IsNullOrWhiteSpace(UserName))
        {
            filtered = filtered.Where(s => string.Equals(s.User, UserName, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(Strategy))
        {
            filtered = filtered.Where(s => string.Equals(s.Strategy, Strategy, StringComparison.OrdinalIgnoreCase));
        }

        if (Players.HasValue)
        {
            filtered = filtered.Where(s => s.Players == Players.Value);
        }

        Scores = filtered.OrderByDescending(s => s.Length).ToList();

        ChartDataJson = JsonSerializer.Serialize(
            all.Select(s => new { x = s.Submitted.ToString("o"), y = s.Length, strategy = s.Strategy ?? "unknown" }));
    }
}
