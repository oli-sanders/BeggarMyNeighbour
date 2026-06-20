using System;
using System.Collections.Generic;
using System.Linq;
using CardGames;

namespace CardGames.BeggarMyNeighbour.Compute
{
    /// <summary>
    /// Helpers for the structural search representation.
    ///
    /// A "genome" is an ordered list of the 16 picture cards (4×Jack=1, 4×Queen=2,
    /// 4×King=3, 4×Ace=4).  The 36 number cards are placed randomly in the gaps
    /// between picture cards each time a full deck is built.  This lets all search
    /// algorithms optimise only the part of the deck that drives game behaviour.
    /// </summary>
    public static class StructuredDeckUtils
    {
        public static readonly List<int> PictureCards =
            new List<int> { 1, 1, 1, 1, 2, 2, 2, 2, 3, 3, 3, 3, 4, 4, 4, 4 };

        private const int NumberCardCount = 36;

        /// <summary>Returns a randomly ordered list of the 16 picture cards.</summary>
        public static List<int> RandomGenome(Random rng)
            => CardUtils.Shuffle(rng, new List<int>(PictureCards));

        /// <summary>
        /// Builds a full 52-card deck from a picture-card ordering by randomly
        /// distributing the 36 number cards across the 17 gap positions.
        /// </summary>
        public static List<int> BuildDeck(Random rng, List<int> genome)
        {
            var gaps = RandomComposition(rng, NumberCardCount, genome.Count + 1);
            var deck = new List<int>(52);
            for (int i = 0; i <= genome.Count; i++)
            {
                for (int g = 0; g < gaps[i]; g++) deck.Add(0);
                if (i < genome.Count) deck.Add(genome[i]);
            }
            return deck;
        }

        /// <summary>Swap two randomly chosen positions in the genome.</summary>
        public static List<int> Mutate(Random rng, List<int> genome)
        {
            var next = new List<int>(genome);
            int i = rng.Next(next.Count);
            int j = rng.Next(next.Count);
            (next[i], next[j]) = (next[j], next[i]);
            return next;
        }

        /// <summary>
        /// Order Crossover (OX) for multiset permutations.
        /// Copies a random segment from <paramref name="p1"/>, then fills the
        /// remaining slots in the order they appear in <paramref name="p2"/>,
        /// respecting the required card counts.
        /// </summary>
        public static List<int> Crossover(Random rng, List<int> p1, List<int> p2)
        {
            int n = p1.Count;
            int start = rng.Next(n);
            int end = rng.Next(n);
            if (start > end) (start, end) = (end, start);

            var offspring = new int?[n];
            var remaining = new Dictionary<int, int> { { 1, 4 }, { 2, 4 }, { 3, 4 }, { 4, 4 } };

            for (int i = start; i <= end; i++)
            {
                offspring[i] = p1[i];
                remaining[p1[i]]--;
            }

            int slot = 0;
            foreach (var card in p2)
            {
                if (remaining[card] <= 0) continue;
                while (slot <= end && offspring[slot].HasValue) slot++;
                if (slot >= n) break;
                offspring[slot] = card;
                remaining[card]--;
                slot++;
            }

            return offspring.Select(x => x!.Value).ToList();
        }

        /// <summary>
        /// Evaluate a genome by playing <paramref name="trials"/> games with
        /// independently randomised gap distributions and returning the best score.
        /// </summary>
        public static int EvaluateBest(Random rng, List<int> genome, int players, int trials = 3, int maxMoves = int.MaxValue)
        {
            int best = 0;
            for (int t = 0; t < trials; t++)
            {
                var deck = BuildDeck(rng, genome);
                int score = new BeggarMyNeighbour.Game(deck, players).Play(maxMoves);
                if (score > best) best = score;
            }
            return best;
        }

        // Distribute n items into k buckets by choosing k-1 random cut points.
        private static int[] RandomComposition(Random rng, int n, int k)
        {
            var cuts = new int[k + 1];
            cuts[0] = 0;
            cuts[k] = n;
            for (int i = 1; i < k; i++)
                cuts[i] = rng.Next(0, n + 1);
            Array.Sort(cuts);
            var gaps = new int[k];
            for (int i = 0; i < k; i++)
                gaps[i] = cuts[i + 1] - cuts[i];
            return gaps;
        }
    }
}
