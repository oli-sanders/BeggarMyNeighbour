using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Scoreboard.API.Services
{
    public class ThresholdData
    {

        public int Players { get; private set; }

        private int _currentthreshold;

        public int Threshold => _currentthreshold;

        private List<int> _currentList;

        public ThresholdData(List<int> currentlist, int players)
        {
            Players = players;
            _currentList = currentlist;
            _currentthreshold = _currentList.LastOrDefault();
        }


        public int UpdateThreshold(int length)
        {
            int current = _currentthreshold;
            if (current < length)
            {
                _currentList.Add(length);
                _currentList = _currentList.OrderByDescending(r => r).Take(10).ToList();
                current = _currentList.LastOrDefault();
                Interlocked.Exchange(ref _currentthreshold, current);
            }
            return current;
        }
    }

    public class ThresholdService
    {
        private ScoreBoardContext _context;

        public ThresholdService(ScoreBoardContext context)
        {
            _context = context;

            _thresholds = new List<ThresholdData>();

            //get number of players
            var q = (from a in _context.Scores
                     group a by a.Players into playergroups
                     select playergroups.Key).ToList();

            //setup thresholds
            foreach (var numberofplayers in q)
            {
                var currentlist = _context.Scores.Where(p => p.Players == numberofplayers).OrderByDescending(r => r.Lenght).Take(10).Select(r => r.Lenght).ToList();
                var current = new ThresholdData(currentlist, numberofplayers);
                _thresholds.Add(current);
            }
        }

        private List<ThresholdData> _thresholds;

        public int UpdateThreshold(int lenght, int players)
        {
            var current = _thresholds.Where(t => t.Players == players).FirstOrDefault();
            
            if(current != null)
            {
               return current.UpdateThreshold(lenght);
            }
            else
            {
                var newList = new List<int> { lenght };
                var newThreshold = new ThresholdData(newList, players);
                _thresholds.Add(newThreshold);
                return lenght;
            }
         }
    }

}
