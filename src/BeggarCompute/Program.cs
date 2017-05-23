using System;
using System.Collections.Generic;
using System.Linq;
using BeggarMyNeighbour;
using Microsoft.Extensions.Logging;

namespace beggar
{
    /// <summary>
    /// Main Program Class runs beggar according to enviromental varibles or default
    /// </summary>
    class Program
    {
        /// <summary>
        /// Main Entrypoint
        /// </summary>
        /// <param name="args"></param>
        static void Main(string[] args)
        {
            var algorithm = Environment.GetEnvironmentVariable("Algorithm") ?? "Best";

            ILoggerFactory loggerFactory = new LoggerFactory();

            loggerFactory
                .AddConsole()
                .AddDebug();

            var players = 4;
            
            switch(algorithm)
            {
                case "Best":
                    var compute = new BindBeggarAlgorithm(loggerFactory.CreateLogger("Compute"),new Random() ,players);
                    compute.Run();
                    break;

            }
        }
    }

  public abstract class BeggarAlgorithm
    {
        private ILogger _logger;
        private int _players;
        private Random _rng;
        private int _threshold;

        public BeggarAlgorithm(ILogger logger, Random rng, int players)
        {
            _logger = logger;
            _players = players;
            _rng = rng;
            //TODO: update threshold
            _threshold = 3500;
        }

        public ILogger Logger => _logger;

        public Random Rng => _rng;

        public int Players => _players;

        public int Threshold => _threshold;
        
    }

   public class BindBeggarAlgorithm : BeggarAlgorithm
    {

       public BindBeggarAlgorithm(ILogger logger, Random rng, int players) : base(logger, rng, players)
        {

        }

       public void Run()
        {
            Logger.LogInformation("I'm Running");

            while (true)
            {
                var shuffledeck = CardUtils.Shuffle(Rng, CardUtils.Deck);
                var ndgame = new Game(shuffledeck.ToList(), Players);

                var result = ndgame.Play();

                //record long games
                if (result > Threshold)
                {
                    var mresult = new GameResult() { Max = result, Deck = shuffledeck };
                    var output = Newtonsoft.Json.JsonConvert.SerializeObject(mresult);
                    System.IO.File.AppendAllText(@"C:\Users\oli\Desktop\Games.txt", output + ",\r\n");
                    Logger.LogInformation("found game of lenght {0} : {1}", result, Newtonsoft.Json.JsonConvert.SerializeObject(shuffledeck));
                }

            }
        }

    }

}