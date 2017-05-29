using System;
using System.Collections.Generic;
using BeggarMyNeighbour;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using System.Net.Http.Headers;
using System.Net.Http;

namespace beggar
{
    public abstract class BeggarAlgorithm
    {
        private ILogger _logger;
        private int _players;
        private Random _rng;
        private int _threshold;
        private string _user;

        public BeggarAlgorithm(ILogger logger, Random rng, int players, string user)
        {
            _logger = logger;
            _players = players;
            _rng = rng;
            //TODO: update threshold
            _threshold = 2000;
            _user = user;
        }

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
            t.Wait();
            _threshold = t.Result;
        }

        private static async Task<int> SendResult(GameResult result, string scoreBoardUri)
        {
            var client = new HttpClient();
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));
            client.DefaultRequestHeaders.Add("User-Agent", "Beggar Reporter");

            var output = Newtonsoft.Json.JsonConvert.SerializeObject(result);
            var content = new System.Net.Http.StringContent(output,System.Text.Encoding.UTF8,"application/json");
            var SendTask = client.PostAsync(scoreBoardUri,content);

            var msg = await SendTask;
            Console.Write(msg);

           var txt = await msg.Content.ReadAsStringAsync();
           var routput = int.Parse(txt);
            return routput;
        }

    }

}