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
using System.Threading;
using CardGames.BeggarMyNeighbour;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using System.Net.Http.Headers;
using System.Net.Http;
using Polly;

namespace CardGames.BeggarMyNeighbour.Compute
{
    /// <summary>
    /// Abstract Base Class for Beggar my neighbour algorithms.
    /// </summary>
    /// <remarks>
    /// Contains threshold management and high score submission functions
    /// </remarks>
    public abstract class BeggarAlgorithm
    {
        /// <summary>
        /// store logger to write out to
        /// </summary>
        private ILogger _logger;

        /// <summary>
        /// store number of players
        /// </summary>
        private int _players;

        /// <summary>
        /// Store random number generator
        /// </summary>
        private Random _rng;

        /// <summary>
        /// Store current threshold
        /// </summary>
        /// <remarks>
        /// set default to 2000 wil be replaced on first submission.
        /// </remarks>
        private volatile int _threshold = 2000;
        private int _bestSubmitted = 0;
        private long _iteration = 0;
        private readonly object _submitLock = new object();

        /// <summary>
        /// store username to submit scores as
        /// </summary>
        private string _user;

        /// <summary>
        /// Scoreboard url to submit to.
        /// </summary>
        private string _scoreboardurl;

        /// <summary>
        /// Version of this compute client (e.g. "1.4.6").
        /// </summary>
        private string _version;

        /// <summary>
        /// Identifier unique to this running compute instance.
        /// </summary>
        private string _instanceId;

        private string _team;

        public BeggarAlgorithm(ILogger logger, Random rng, int players, string user, string scoreboardUrl, string version, string instanceId, string team = null)
        {
            _logger = logger;
            _players = players;
            _rng = rng;
            _user = user;
            _scoreboardurl = scoreboardUrl;
            _version = version;
            _instanceId = instanceId;
            _team = team;
        }

        /// <summary>
        /// Name of the strategy this algorithm uses (e.g. "brute-force").
        /// </summary>
        public abstract string Strategy { get; }

        /// <summary>
        /// Fetches the current threshold from the scoreboard for this algorithm's
        /// (players, strategy) bucket and updates the local threshold accordingly.
        /// Falls back to the existing value if the scoreboard is unreachable.
        /// </summary>
        private void FetchThreshold()
        {
            try
            {
                var thresholdUrl = $"{_scoreboardurl}/threshold?players={_players}&strategy={Uri.EscapeDataString(Strategy)}";
                using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
                var result = client.GetStringAsync(thresholdUrl).Result;
                if (int.TryParse(result, out var t))
                {
                    _threshold = t;
                    _logger.LogInformation("Fetched initial threshold for {Strategy}/{Players}p: {Threshold}", Strategy, _players, t);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not fetch threshold for {Strategy}/{Players}p, using default {Default}", Strategy, _players, _threshold);
            }
        }

        public void Run(CancellationToken cancellationToken = default)
        {
            FetchThreshold();
            DoRun(cancellationToken);
        }

        protected abstract void DoRun(CancellationToken cancellationToken);

        /// <summary>
        /// Expose logger to derived
        /// </summary>
        public ILogger Logger => _logger;

        public Random Rng => _rng;

        public int Players => _players;

        public int Threshold => _threshold;
        public int BestSubmitted => _bestSubmitted;

        public long Iteration => _iteration;

        public void IncrementIteration() => Interlocked.Increment(ref _iteration);

        public string User => _user;

        public string Version => _version;

        public string InstanceId => _instanceId;

        public string ScoreboardUrl => _scoreboardurl;

        public string Team => _team;

        public void SubmitGame(List<int> deck, int length)
        {
            lock (_submitLock)
            {
                if (length <= _bestSubmitted)
                    return;
                _bestSubmitted = length;
            }

            var mresult = new GameResult()
            {
                User = User,
                Length = length,
                Deck = deck,
                Players = Players,
                Version = Version,
                Strategy = Strategy,
                InstanceId = InstanceId,
                Team = Team,
                Iteration = Interlocked.Read(ref _iteration)
            };
            Logger.LogInformation("found game of length {0} : {1}", length, Newtonsoft.Json.JsonConvert.SerializeObject(deck));
            var t = HttpSendResult(mresult, _scoreboardurl);
            _threshold = t;
        }

        private int HttpSendResult(GameResult result, string scoreBoardUri)
        {
            var RetryPolicy = Policy.Handle<HttpRequestException>().WaitAndRetry(11, retryAttempt => TimeSpan.FromMilliseconds(Math.Pow(2, retryAttempt) * 500));
            var output = Newtonsoft.Json.JsonConvert.SerializeObject(result);

            var pollyresult = RetryPolicy.ExecuteAndCapture(() => SendResult(scoreBoardUri, output));

            if (pollyresult.Outcome == OutcomeType.Failure)
            {
                return result.Length;
            }

            return pollyresult.Result;
        }

        private int SendResult(string url, string JSONGame)
        {
            var client = new HttpClient();
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));
            client.DefaultRequestHeaders.Add("User-Agent", "Beggar Reporter");
            
            var content = new System.Net.Http.StringContent(JSONGame, System.Text.Encoding.UTF8, "application/json");
            var SendTask = client.PostAsync(url, content);

            SendTask.Wait();
            var msg = SendTask.Result;
            Console.Write(msg);

            var txttask = msg.Content.ReadAsStringAsync();
            txttask.Wait();
            var txt = txttask.Result;
            var routput = int.Parse(txt);
            return routput;
        }
    }
}
