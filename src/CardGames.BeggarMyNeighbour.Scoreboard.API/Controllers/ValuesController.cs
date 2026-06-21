/* Copyright (c) 2017 Oliver Sanders

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
*/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using RabbitMQ.Client;
using System.Text;
using CardGames.BeggarMyNeighbour.Scoreboard.Models;
using CardGames.BeggarMyNeighbour.Scoreboard.API.Services;

namespace CardGames.BeggarMyNeighbour.Scoreboard.API.Controllers
{
    [Route("api/[controller]")]
    public class ScoresController : Controller
    {
        private ScoreBoardContext _context;
        private ThresholdService _thresholdService;
        private IConnectionFactory _connectionFactory;
        private readonly UpstreamScoreboardService _upstream;
        private readonly ILogger<ScoresController> _logger;
        private readonly bool _skipVerification;

        public ScoresController(ScoreBoardContext context, ThresholdService threshold, IConnectionFactory connectionFactory, UpstreamScoreboardService upstream, IConfiguration configuration, ILogger<ScoresController> logger)
        {
            _context = context;
            _thresholdService = threshold;
            _connectionFactory = connectionFactory;
            _upstream = upstream;
            _logger = logger;
            _skipVerification = string.Equals(
                configuration["SkipVerification"], "true",
                StringComparison.OrdinalIgnoreCase);

            if (_skipVerification)
                _logger.LogWarning("Score verification is DISABLED (SkipVerification=true). Scores will be marked unverified.");
        }

        // GET api/scores/threshold?players=4&strategy=hill-climb
        [HttpGet("threshold")]
        public IActionResult GetThreshold([FromQuery] int players, [FromQuery] string strategy)
        {
            if (players < 2) return BadRequest("players must be >= 2");
            if (string.IsNullOrEmpty(strategy)) return BadRequest("strategy is required");
            return Ok(_thresholdService.GetThreshold(players, strategy));
        }

        // GET api/scores/history?players=4&limit=5000
        // Returns scores ordered by submission time (oldest first) for charting historical progress.
        // Downsampled: returns at most one score per minute per strategy (the best in that bucket).
        [HttpGet("history")]
        public IActionResult GetHistory([FromQuery] int? players = null, [FromQuery] int limit = 5000)
        {
            if (limit < 1 || limit > 20000) limit = 5000;

            var query = _context.Scores.AsQueryable();
            if (players.HasValue)
                query = query.Where(s => s.Players == players.Value);

            // Pull all scores ordered by time, then downsample in memory to keep the
            // response small: keep the best score per (strategy, minute) bucket.
            var raw = query
                .OrderBy(s => s.Submitted)
                .Select(s => new { s.Strategy, s.Submitted, s.Length })
                .ToList();

            var seen = new Dictionary<string, int>(); // key: "strategy|yyyyMMddHHmm", value: best Length
            var sampled = new List<(string Strategy, DateTime Submitted, int Length)>();

            foreach (var r in raw)
            {
                var bucket = $"{r.Strategy}|{r.Submitted:yyyyMMddHHmm}";
                if (!seen.TryGetValue(bucket, out var best) || r.Length > best)
                {
                    seen[bucket] = r.Length;
                    sampled.RemoveAll(x => x.Strategy == r.Strategy &&
                                          x.Submitted.ToString("yyyyMMddHHmm") == r.Submitted.ToString("yyyyMMddHHmm"));
                    sampled.Add((r.Strategy, r.Submitted, r.Length));
                }
            }

            var result = sampled
                .OrderBy(x => x.Submitted)
                .Take(limit)
                .Select(x => new { strategy = x.Strategy, submitted = x.Submitted, length = x.Length })
                .ToList();

            return Ok(result);
        }

        // GET api/scores?page=1&pageSize=100&players=4
        [HttpGet]
        public IActionResult Get([FromQuery] int page = 1, [FromQuery] int pageSize = 100, [FromQuery] int? players = null)
        {
            if (page < 1) page = 1;
            if (pageSize < 1 || pageSize > 1000) pageSize = 100;

            var query = _context.Scores.OrderByDescending(s => s.Length).AsQueryable();
            if (players.HasValue)
                query = query.Where(s => s.Players == players.Value);

            var scores = query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(s => s.ToScoreResponse())
                .ToList();
            return Ok(scores);
        }

        // GET api/values/5
        [HttpGet("{id}")]
        public string Get(int id)
        {
            if(id == 5)
            {
                var unverifiedscores = _context.Scores.Where(s => s.Verified == null).ToList();
                foreach(var uv in unverifiedscores)
                {
                    SendGameToVerify(uv);
                }
            }

            return "value";
        }

        // POST api/values
        [HttpPost]
        [EnableRateLimiting("post-scores")]
        public async Task<IActionResult> Post([FromBody]ScoreRequest value)
        {
            if (ModelState.IsValid)
            {
                //fix values
                var dbvalue = value.ToScore();

                var current = _thresholdService.UpdateThreshold(dbvalue.Length, dbvalue.Players, dbvalue.Strategy);

                //add and save to db, ignoring exact duplicates (same deck + players)
                _context.Scores.Add(dbvalue);
                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateException)
                {
                    // Duplicate deck — return the current threshold without recording.
                    _logger.LogInformation("Duplicate deck submission ignored for deck with {Length} moves", dbvalue.Length);
                    return Ok(current);
                }

                if (!_skipVerification)
                    SendGameToVerify(dbvalue);

                // Forward to upstream scoreboard if this score qualifies for the local leaderboard.
                if (dbvalue.Length >= current)
                    _upstream.ForwardInBackground(value);

                return Ok(current);
            }
            else
            {
                return BadRequest(ModelState);
            }
        }

        private void SendGameToVerify(Score dbvalue)
        {
            try
            {
                using var connection = _connectionFactory.CreateConnection();
                using var channel = connection.CreateModel();
                channel.QueueDeclare(queue: "verify_queue", durable: true, exclusive: false, autoDelete: false, arguments: null);

                var message = Newtonsoft.Json.JsonConvert.SerializeObject(dbvalue.ToVerifyRequest());
                var body = Encoding.UTF8.GetBytes(message);

                var properties = channel.CreateBasicProperties();
                properties.Persistent = true;

                channel.BasicPublish(exchange: "", routingKey: "verify_queue", basicProperties: properties, body: body);
                _logger.LogInformation("Queued game {Id} for verification", dbvalue.id);
            }
            catch (Exception ex)
            {
                // A submitted score is still recorded even if the verify bus is
                // unavailable; it simply stays unverified until re-queued.
                _logger.LogWarning(ex, "Could not queue game {Id} for verification", dbvalue.id);
            }
        }

        // DELETE api/values/5
        [HttpDelete("{id}")]
        public void Delete(int id)
        {
        }
    }
}
