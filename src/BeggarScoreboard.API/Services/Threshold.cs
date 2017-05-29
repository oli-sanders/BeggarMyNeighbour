using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Scoreboard.API.Services
{
    public class ThresholdService
    {
        private ScoreBoardContext _context;

        public ThresholdService(ScoreBoardContext context)
        {
            _context = context;
            SetupThreshold();
        }

        private int _currentthreshold;

        public int Threshold => _currentthreshold;

        private List<int> _currentList; 
        
        void SetupThreshold()
        {
            _currentList = _context.Scores.OrderByDescending(r => r.Lenght).Take(10).Select(r => r.Lenght).ToList();
         var current = _currentList.LastOrDefault();
            Interlocked.Exchange(ref _currentthreshold, current);
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
}
