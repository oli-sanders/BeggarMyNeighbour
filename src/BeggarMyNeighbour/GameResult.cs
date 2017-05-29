using System.Collections.Generic;

namespace BeggarMyNeighbour
{
    public class GameResult
    {
        public string User { get; set; }
        public int Lenght { get; set; }
        public List<int> Deck { get; set; }
        public int Players { get; set; }
    }
}