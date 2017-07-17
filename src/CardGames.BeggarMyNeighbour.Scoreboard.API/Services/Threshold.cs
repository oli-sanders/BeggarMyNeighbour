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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CardGames.BeggarMyNeighbour.Scoreboard.API.Services
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
