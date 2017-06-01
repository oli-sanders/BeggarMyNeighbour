using System;
using System.Collections.Generic;
using BeggarMyNeighbour;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using System.Net.Http.Headers;
using System.Net.Http;

namespace beggar
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


        public BeggarAlgorithm(ILogger logger, Random rng, int players, string user)
        {
            _logger = logger;
            _players = players;
            _rng = rng;
            _user = user;
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
            System.IO.File.AppendAllText(@"C:\Users\oli\Desktop\Games.txt", output + ",\r\n");
            Logger.LogInformation("found game of lenght {0} : {1}", lenght, Newtonsoft.Json.JsonConvert.SerializeObject(deck));
            var t = SendResult(mresult, "http://localhost:54056/api/scores");
            
            _threshold = t;
        }

        private int SendResult(GameResult result, string scoreBoardUri)
        {
            var client = new HttpClient();
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));
            client.DefaultRequestHeaders.Add("User-Agent", "Beggar Reporter");

            var output = Newtonsoft.Json.JsonConvert.SerializeObject(result);
            var content = new System.Net.Http.StringContent(output,System.Text.Encoding.UTF8,"application/json");
            var SendTask = client.PostAsync(scoreBoardUri,content);

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