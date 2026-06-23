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

namespace CardGames.BeggarMyNeighbour.Scoreboard.Models
{
    public class ScoreResponse
    {
        public string User { get; set; }

        /// <summary>
        /// The score for the game: the number of moves (cards played) before it ended.
        /// </summary>
        public int Length { get; set; }

        public System.DateTime Submitted { get; set; }

        public List<int> Deck { get; set; }
        public bool IsVerified { get; set; }
        public int Players { get; set; }

        /// <summary>
        /// Version of the compute client that found this game (e.g. "1.4.6").
        /// </summary>
        public string Version { get; set; }

        /// <summary>
        /// Name of the strategy used to find this game (e.g. "brute-force").
        /// </summary>
        public string Strategy { get; set; }

        /// <summary>
        /// Identifier of the compute instance that found this game.
        /// </summary>
        public string InstanceId { get; set; }

        /// <summary>
        /// Team name the score was submitted under (e.g. "uni-lab").
        /// </summary>
        public string Team { get; set; }

        public long? Iteration { get; set; }
    }
}