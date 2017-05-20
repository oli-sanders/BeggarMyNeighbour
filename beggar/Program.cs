using System;
using System.Collections.Generic;
using System.Linq;
using BeggarMyNeighbour;
using Microsoft.Extensions.Logging;

namespace beggar
{
    class Program
    {
        static void Main(string[] args)
        {
            ILoggerFactory loggerFactory = new LoggerFactory();

                loggerFactory
                    .AddConsole()
                    .AddDebug();

                var players = 4;
            List<int> newdeck = CardUtils.Deck;

            //always play the calibration game should take 105 moves
            //var game = new Game(loggerFactory.CreateLogger("Game"), newdeck.ToList(), players);
            var game = new Game(newdeck.ToList(), players);
            var result = game.Play();
            var max = result;
            var games = 1;
            double avg = result;
            var RNG = new Random();
            Dictionary<int, int> groups = new Dictionary<int, int>();

            while (true)
            {
                games++;
                var shuffledeck = CardUtils.Shuffle(RNG, newdeck.ToList());
//                var ndgame = new Game(loggerFactory.CreateLogger($"Game {games}"), shuffledeck.ToList(), players);
                var ndgame = new Game(shuffledeck.ToList(), players);

                result = ndgame.Play();

                avg = avg + (result - avg) / games;
                
                    var currentgroup = (int)Math.Floor(result / 250.0) * 250;
                    
                    if (groups.ContainsKey(currentgroup))
                    {
                        groups[currentgroup]++;
                    }
                    else
                    {
                        groups.Add(currentgroup, 1);
                    }

                //record max lenght games
                if (result > max)
                {
                    max = result;
                }

                //record long games
                if(result >2750)
                { 
                    var mresult = new GameResult() { Max = result, Deck = shuffledeck };
                    var output = Newtonsoft.Json.JsonConvert.SerializeObject(mresult);
                    System.IO.File.AppendAllText(@"C:\Users\oli\Desktop\Games.txt", output + ",\r\n");
                }

                if (games % 1000000 == 0)
                {
                    Console.WriteLine($"Game {games} : Last result {result}, Max {max}, Avg {avg}");

                    var output = Newtonsoft.Json.JsonConvert.SerializeObject(groups);
                    System.IO.File.WriteAllText(@"C:\Users\oli\Desktop\GamesDistribution.txt", output + ",\r\n");
                }

                if (games % 100000000 == 0)
                {
                    var i = 0;
                    while (groups.ContainsKey(i))
                    {
                        Console.WriteLine($"Games Between {i} - {i + 250} : {groups[i]}");
                        i = i + 250;
                    }
                }
            }
        }
    }

}