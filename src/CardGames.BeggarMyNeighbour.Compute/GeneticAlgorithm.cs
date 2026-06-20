using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using CardGames.BeggarMyNeighbour;
using Microsoft.Extensions.Logging;

namespace CardGames.BeggarMyNeighbour.Compute
{
    public class GeneticAlgorithm : BeggarAlgorithm
    {
        private const int PopulationSize = 100;
        private const double BaseMutationRate = 0.1;
        private const double MaxMutationRate = 0.5;
        private const int StagnationLimit = 50;      // generations before boosting mutation
        private const int ImmigrationInterval = 100; // generations between scoreboard imports
        private const int ImmigrantCount = 5;        // individuals replaced per import

        // Thread-local RNG so parallel fitness evaluation is safe.
        private static readonly ThreadLocal<Random> _threadRng =
            new ThreadLocal<Random>(() => new Random(Guid.NewGuid().GetHashCode()));

        public GeneticAlgorithm(ILogger logger, Random rng, int players, string user, string scoreboardUrl, string version, string instanceId, string team = null)
            : base(logger, rng, players, user, scoreboardUrl, version, instanceId, team) { }

        public override string Strategy => "genetic";

        public void Run()
        {
            Logger.LogInformation("Genetic algorithm (structural) starting. Population={N}", PopulationSize);

            var population = Enumerable.Range(0, PopulationSize)
                .Select(_ => StructuredDeckUtils.RandomGenome(Rng))
                .ToList();

            int generation = 0;
            int bestEver = 0;
            int stagnant = 0;
            double mutationRate = BaseMutationRate;

            while (true)
            {
                // Parallel fitness evaluation — each thread uses its own RNG.
                var scored = population
                    .AsParallel()
                    .Select(g => (genome: g, score: StructuredDeckUtils.EvaluateBest(_threadRng.Value, g, Players)))
                    .OrderByDescending(x => x.score)
                    .ToList();

                var best = scored[0];

                // Submit if we've beaten the threshold.
                if (best.score > Threshold)
                {
                    var deck = StructuredDeckUtils.BuildDeck(Rng, best.genome);
                    SubmitGame(deck, new Game(deck, Players).Play());
                }

                // Adaptive mutation rate.
                if (best.score > bestEver)
                {
                    bestEver = best.score;
                    stagnant = 0;
                    mutationRate = BaseMutationRate;
                }
                else
                {
                    stagnant++;
                    mutationRate = Math.Min(MaxMutationRate,
                        BaseMutationRate + (double)stagnant / StagnationLimit * (MaxMutationRate - BaseMutationRate));
                }

                // Island model: occasionally import top decks from the scoreboard.
                if (generation % ImmigrationInterval == 0)
                {
                    var immigrants = FetchImmigrants();
                    if (immigrants.Count > 0)
                    {
                        Logger.LogInformation("Generation {Gen}: importing {N} immigrants from scoreboard", generation, immigrants.Count);
                        for (int i = 0; i < Math.Min(immigrants.Count, ImmigrantCount); i++)
                            scored[scored.Count - 1 - i] = (genome: immigrants[i], score: 0);
                    }
                }

                // Select top half as parents.
                var parents = scored.Take(PopulationSize / 2).Select(x => x.genome).ToList();

                // Build next generation.
                var nextGen = new List<List<int>>(PopulationSize);

                // Elitism: carry over top 10%.
                foreach (var p in parents.Take(PopulationSize / 10))
                    nextGen.Add(p);

                // Fill the rest with crossover + mutation.
                while (nextGen.Count < PopulationSize)
                {
                    var p1 = parents[Rng.Next(parents.Count)];
                    var p2 = parents[Rng.Next(parents.Count)];
                    var child = StructuredDeckUtils.Crossover(Rng, p1, p2);
                    if (Rng.NextDouble() < mutationRate)
                        child = StructuredDeckUtils.Mutate(Rng, child);
                    nextGen.Add(child);
                }

                population = nextGen;
                generation++;

                if (generation % ImmigrationInterval == 0)
                    Logger.LogInformation("Generation {Gen}, best this cycle: {Score}, mutation rate: {Rate:P0}, stagnant: {Stagnant}",
                        generation, best.score, mutationRate, stagnant);
            }
        }

        /// <summary>
        /// Fetch the top games from the scoreboard and extract their picture-card genomes.
        /// Falls back to an empty list if the scoreboard is unreachable.
        /// </summary>
        private List<List<int>> FetchImmigrants()
        {
            try
            {
                using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
                var json = client.GetStringAsync(ScoreboardUrl).Result;
                var array = Newtonsoft.Json.Linq.JArray.Parse(json);
                return array
                    .Take(20)
                    .Select(item => item["Deck"]?.ToObject<List<int>>())
                    .Where(deck => deck != null && deck.Count == 52)
                    .Select(deck => deck.Where(c => c > 0).ToList())
                    .Where(g => g.Count == 16)
                    .Take(ImmigrantCount)
                    .ToList();
            }
            catch (Exception ex)
            {
                Logger.LogDebug(ex, "Could not fetch immigrants from scoreboard");
                return new List<List<int>>();
            }
        }
    }
}
