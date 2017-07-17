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
        private int _threshold = 2000;

        /// <summary>
        /// store username to submit scores as
        /// </summary>
        private string _user;

        /// <summary>
        /// Scoreboard url to submit to.
        /// </summary>
        private string _scoreboardurl;

        public BeggarAlgorithm(ILogger logger, Random rng, int players, string user, string scoreboardUrl)
        {
            _logger = logger;
            _players = players;
            _rng = rng;
            _user = user;
            _scoreboardurl = scoreboardUrl;
        }

        /// <summary>
        /// Expose logger to derived 
        /// </summary>
        public ILogger Logger => _logger;

        public Random Rng => _rng;

        public int Players => _players;

        public int Threshold => _threshold;

        public string User => _user;

        public void SubmitGame(List<int> deck, int lenght)
        {
            var mresult = new GameResult() { User = User, Lenght = lenght, Deck = deck, Players = Players };
            var output = Newtonsoft.Json.JsonConvert.SerializeObject(mresult);
            Logger.LogInformation("found game of lenght {0} : {1}", lenght, Newtonsoft.Json.JsonConvert.SerializeObject(deck));
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
                return result.Lenght;
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