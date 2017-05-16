using System;
using System.Collections.Generic;
using System.Linq;

namespace BeggarMyNeighbour
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
        public static List<int> Shuffle(List<int> deck)
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
            { return new List<int> { 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 2, 3, 4, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 2, 3, 4, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 2, 3, 4, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 2, 3, 4 };
            }
        }
    }
}