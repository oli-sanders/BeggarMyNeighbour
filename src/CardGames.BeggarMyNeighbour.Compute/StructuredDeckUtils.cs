using System;
using System.Collections.Generic;
using System.Linq;
using CardGames;

namespace CardGames.BeggarMyNeighbour.Compute
{
    /// <summary>
    /// Helpers for the structural search representation.
    ///
    /// A "genome" is a 33-element list encoding the full deck structure:
    ///   [0..15]  — ordered picture cards (4×Jack=1, 4×Queen=2, 4×King=3, 4×Ace=4)
    ///   [16..32] — 17 gap sizes (number of zero-cards before each picture card and
    ///              after the last), always summing to 36.
    ///
    /// This makes BuildDeck deterministic: every evaluation of the same genome plays
    /// the same deck, so no multi-trial averaging is needed.
    /// </summary>
    public static class StructuredDeckUtils
    {
        public static readonly List<int> PictureCards =
            new List<int> { 1, 1, 1, 1, 2, 2, 2, 2, 3, 3, 3, 3, 4, 4, 4, 4 };

        private const int PictureCount = 16;
        private const int NumberCardCount = 36;
        private const int GapCount = PictureCount + 1; // 17 gaps

        /// <summary>Returns a random genome: shuffled picture cards followed by random gap sizes.</summary>
        public static List<int> RandomGenome(Random rng)
        {
            var pictures = CardUtils.Shuffle(rng, new List<int>(PictureCards));
            var gaps = RandomComposition(rng, NumberCardCount, GapCount);
            return pictures.Concat(gaps).ToList();
        }

        /// <summary>
        /// Builds a deterministic 52-card deck from a genome.
        /// Genome layout: [0..15] picture order, [16..32] gap sizes.
        /// </summary>
        public static List<int> BuildDeck(List<int> genome)
        {
            var deck = new List<int>(52);
            for (int i = 0; i < GapCount; i++)
            {
                int gapSize = genome[PictureCount + i];
                for (int g = 0; g < gapSize; g++) deck.Add(0);
                if (i < PictureCount) deck.Add(genome[i]);
            }
            return deck;
        }

        /// <summary>
        /// Reconstructs a genome from a full 52-card deck.
        /// Returns null if the deck does not contain exactly 16 picture cards.
        /// </summary>
        public static List<int>? DeckToGenome(List<int> deck)
        {
            var pictures = new List<int>();
            var gaps = new List<int>();
            int currentGap = 0;

            foreach (var card in deck)
            {
                if (card == 0)
                {
                    currentGap++;
                }
                else
                {
                    gaps.Add(currentGap);
                    pictures.Add(card);
                    currentGap = 0;
                }
            }
            gaps.Add(currentGap); // trailing gap

            if (pictures.Count != PictureCount) return null;
            return pictures.Concat(gaps).ToList();
        }

        /// <summary>
        /// Heuristic mutation: applies one of three targeted operators derived from
        /// pattern analysis of top-scoring decks, falling back to the standard operator
        /// 25% of the time.
        ///   - Ace-avoidance  (25%): moves an Ace from the high end of the picture
        ///     sequence (positions 12-15) to a lower position. Aces clustered at the
        ///     end correlate strongly with lower scores.
        ///   - K→Q promotion  (25%): swaps the card after a King with a Queen from
        ///     elsewhere, creating the King-then-Queen adjacency that correlates with
        ///     longer payment chains.
        ///   - Gap redistribution (25%): moves a number card from the first-third of
        ///     gaps (0-5) into the middle third (6-10). Top scorers have fewer number
        ///     cards front-loaded in the deck.
        ///   - Standard Mutate (25%): preserves normal search diversity.
        /// </summary>
        public static List<int> HeuristicMutate(Random rng, List<int> genome)
        {
            double roll = rng.NextDouble();
            if (roll < 0.25) return AceSpreadMutate(rng, genome);
            if (roll < 0.50) return KingQueenMutate(rng, genome);
            if (roll < 0.75) return GapRedistributeMutate(rng, genome);
            return Mutate(rng, genome);
        }

        private static List<int> AceSpreadMutate(Random rng, List<int> genome)
        {
            var next = new List<int>(genome);
            var highAces = Enumerable.Range(12, 4).Where(i => next[i] == 4).ToList();
            if (highAces.Count == 0) return Mutate(rng, genome);
            int from = highAces[rng.Next(highAces.Count)];
            int to = rng.Next(12);
            (next[from], next[to]) = (next[to], next[from]);
            return next;
        }

        private static List<int> KingQueenMutate(Random rng, List<int> genome)
        {
            var next = new List<int>(genome);
            var targetKings = Enumerable.Range(0, PictureCount - 1)
                .Where(i => next[i] == 3 && next[i + 1] != 2)
                .ToList();
            if (targetKings.Count == 0) return Mutate(rng, genome);
            int kingPos = targetKings[rng.Next(targetKings.Count)];
            var queens = Enumerable.Range(0, PictureCount)
                .Where(i => next[i] == 2 && i != kingPos + 1)
                .ToList();
            if (queens.Count == 0) return Mutate(rng, genome);
            int queenPos = queens[rng.Next(queens.Count)];
            (next[kingPos + 1], next[queenPos]) = (next[queenPos], next[kingPos + 1]);
            return next;
        }

        private static List<int> GapRedistributeMutate(Random rng, List<int> genome)
        {
            var next = new List<int>(genome);
            var sources = Enumerable.Range(PictureCount, 6).Where(i => next[i] > 0).ToList();
            if (sources.Count == 0) return Mutate(rng, genome);
            int from = sources[rng.Next(sources.Count)];
            int to = PictureCount + 6 + rng.Next(5); // gaps 6-10
            next[from]--;
            next[to]++;
            return next;
        }

        /// <summary>Mutates either the picture card order (swap) or the gap distribution (shift).</summary>
        public static List<int> Mutate(Random rng, List<int> genome)
        {
            var next = new List<int>(genome);
            if (rng.NextDouble() < 0.5)
            {
                // Swap two picture card positions.
                int i = rng.Next(PictureCount);
                int j = rng.Next(PictureCount);
                (next[i], next[j]) = (next[j], next[i]);
            }
            else
            {
                // Move one number card from a non-empty gap to any other gap.
                var nonEmpty = Enumerable.Range(PictureCount, GapCount)
                    .Where(k => next[k] > 0)
                    .ToList();
                if (nonEmpty.Count > 0)
                {
                    int from = nonEmpty[rng.Next(nonEmpty.Count)];
                    int to = PictureCount + rng.Next(GapCount);
                    next[from]--;
                    next[to]++;
                }
            }
            return next;
        }

        /// <summary>
        /// Combines two genomes: Order Crossover (OX) on the picture section,
        /// uniform blend (with sum-normalisation) on the gap section.
        /// </summary>
        public static List<int> Crossover(Random rng, List<int> p1, List<int> p2)
        {
            // --- Picture crossover: Order Crossover (OX) ---
            int start = rng.Next(PictureCount);
            int end = rng.Next(PictureCount);
            if (start > end) (start, end) = (end, start);

            var offspring = new int?[PictureCount];
            var remaining = new Dictionary<int, int> { { 1, 4 }, { 2, 4 }, { 3, 4 }, { 4, 4 } };

            for (int i = start; i <= end; i++)
            {
                offspring[i] = p1[i];
                remaining[p1[i]]--;
            }

            int slot = 0;
            foreach (var card in p2.Take(PictureCount))
            {
                if (remaining[card] <= 0) continue;
                while (slot <= end && offspring[slot].HasValue) slot++;
                if (slot >= PictureCount) break;
                offspring[slot] = card;
                remaining[card]--;
                slot++;
            }

            var pictureResult = offspring.Select(x => x!.Value).ToList();

            // --- Gap crossover: uniform blend, then normalise to sum=36 ---
            var childGaps = new int[GapCount];
            for (int i = 0; i < GapCount; i++)
                childGaps[i] = rng.NextDouble() < 0.5 ? p1[PictureCount + i] : p2[PictureCount + i];

            int diff = childGaps.Sum() - NumberCardCount;
            while (diff != 0)
            {
                int i = rng.Next(GapCount);
                if (diff > 0 && childGaps[i] > 0) { childGaps[i]--; diff--; }
                else if (diff < 0) { childGaps[i]++; diff++; }
            }

            return pictureResult.Concat(childGaps).ToList();
        }

        /// <summary>Evaluates a genome by playing one deterministic game.</summary>
        public static int EvaluateBest(List<int> genome, int players, int maxMoves = int.MaxValue)
        {
            var deck = BuildDeck(genome);
            return new BeggarMyNeighbour.Game(deck, players).Play(maxMoves);
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
