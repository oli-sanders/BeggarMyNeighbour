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

namespace CardGames
{
    /// <summary>
    /// Utility functions for Card Games
    /// </summary>
    public static class CardUtils
    {
        /// <summary>
        /// Fisher–Yates or Knuth shuffle
        /// </summary>
        /// <param name="deck">Deck to shuffle</param>
        /// <returns>Shuffled Deck</returns>
        public static List<int> Shuffle(Random rng, List<int> deck)
        {
            var newdeck = new List<int>();
            while (deck.Count > 0)
            {
                var pos = rng.Next(0, deck.Count - 1);
                newdeck.Add(deck[pos]);
                deck.RemoveAt(pos);
            }
            return newdeck;
        }

        /// <summary>
        /// Create a Deck of cards
        /// </summary>
        /// <remarks>
        /// -1 = Jack
        /// -2 = Queen
        /// -3 = King
        /// -4 = Ace
        /// </remarks>
        /// <returns>List of Integers representing a deck of cards without suits</returns>
        public static List<int> Deck { get
            { return new List<int> { 4, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 2, 3, 4, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 2, 3, 4, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 2, 3, 4, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 2, 3 };
            }
        }
    }
}