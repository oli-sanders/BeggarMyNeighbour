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
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.Extensions.DependencyInjection;

namespace CardGames.BeggarMyNeighbour.Scoreboard.API.Services
{
    /// <summary>
    /// Tracks the cut-off score (number of moves) needed to make the leaderboard.
    /// Only the top <see cref="ScoreboardSize"/> games per player count are kept.
    /// </summary>
    public class ThresholdData
    {
        /// <summary>
        /// Number of games to keep on the leaderboard for each player count.
        /// </summary>
        public const int ScoreboardSize = 1000;

        public int Players { get; private set; }

        private int _currentthreshold;

        public int Threshold => _currentthreshold;

        private List<int> _currentList;

        public ThresholdData(List<int> currentlist, int players)
        {
            Players = players;
            _currentList = currentlist.OrderByDescending(r => r).Take(ScoreboardSize).ToList();
            // Once the board is full the threshold is the lowest qualifying score;
            // until then anything beats the (empty) board.
            _currentthreshold = _currentList.Count >= ScoreboardSize ? _currentList.Last() : 0;
        }

        public int UpdateThreshold(int length)
        {
            lock (_currentList)
            {
                if (length > _currentthreshold || _currentList.Count < ScoreboardSize)
                {
                    _currentList.Add(length);
                    _currentList = _currentList.OrderByDescending(r => r).Take(ScoreboardSize).ToList();
                }

                var current = _currentList.Count >= ScoreboardSize ? _currentList.Last() : 0;
                Interlocked.Exchange(ref _currentthreshold, current);
                return current;
            }
        }
    }

    public class ThresholdService
    {
        private readonly List<ThresholdData> _thresholds;

        public ThresholdService(IServiceScopeFactory scopeFactory)
        {
            _thresholds = new List<ThresholdData>();

            using var scope = scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ScoreBoardContext>();

            // Distinct player counts already present in the database.
            var playerCounts = context.Scores
                .Select(s => s.Players)
                .Distinct()
                .ToList();

            foreach (var numberOfPlayers in playerCounts)
            {
                var currentList = context.Scores
                    .Where(p => p.Players == numberOfPlayers)
                    .OrderByDescending(r => r.Length)
                    .Take(ThresholdData.ScoreboardSize)
                    .Select(r => r.Length)
                    .ToList();

                _thresholds.Add(new ThresholdData(currentList, numberOfPlayers));
            }
        }

        public int UpdateThreshold(int length, int players)
        {
            ThresholdData current;
            lock (_thresholds)
            {
                current = _thresholds.FirstOrDefault(t => t.Players == players);
                if (current == null)
                {
                    current = new ThresholdData(new List<int> { length }, players);
                    _thresholds.Add(current);
                    return current.Threshold;
                }
            }

            return current.UpdateThreshold(length);
        }
    }
}
