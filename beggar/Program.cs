using System;
using System.Collections.Generic;
using System.Linq;

namespace beggar
{
    class Program
    {
        static void Main(string[] args)
        {
            var newdeck = new List<int>();
            var players = 4;

            for (int i = 0; i < 4; i++)
            {
                newdeck.AddRange(Enumerable.Range(2, 9).ToList());
                newdeck.AddRange(Enumerable.Range(-4, 4).ToList());
            }

            //always play the calibartion game should take 105 moves
            var game = new Game(newdeck.ToList(), players);
            var result = game.Play();
            var max = result;
            var games = 1;
            double avg = result;
            Dictionary<int, int> groups = new Dictionary<int, int>();

            while (true)
            {
                games++;
                var shuffledeck = shuffle(newdeck.ToList());
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


                if (result > max)
                {
                    max = result;

                    var mresult = new Maxresult() { Game = games, Max = max, Deck = shuffledeck };

                    var output = Newtonsoft.Json.JsonConvert.SerializeObject(mresult);

                    System.IO.File.AppendAllText(@"C:\Users\oli\Desktop\Games.txt", output + ",\r\n");


                }

                if (games % 1000000 == 0)
                {
                    Console.WriteLine($"Game {games} : Last result {result}, Max {max}, Avg {avg}");
                }

                if (games % 100000000 == 0)
                {
                    var i = 0;
                    while(groups.ContainsKey(i))
                    {
                        Console.WriteLine($"Games Between {i} - {i + 250} : {groups[i]}");
                        i = i + 250;


                    }


                    var output = Newtonsoft.Json.JsonConvert.SerializeObject(groups);

                    System.IO.File.WriteAllText(@"C:\Users\oli\Desktop\GamesDistribution.txt", output + ",\r\n");
                }


            }


        }

        class Maxresult
        {
            public int Game { get; set; }
            public int Max { get; set; }
            public List<int> Deck { get; set; }
        }

        private static List<int> shuffle(List<int> deck)
        {
            var RNG = new Random();
            var newdeck = new List<int>();
            while (deck.Count > 0)
            {
                var pos = RNG.Next(0, deck.Count - 1);
                newdeck.Add(deck[pos]);
                deck.RemoveAt(pos);
            }
            return newdeck;
        }
    }
}