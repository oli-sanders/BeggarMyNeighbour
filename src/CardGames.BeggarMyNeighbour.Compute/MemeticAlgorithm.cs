using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using CardGames.BeggarMyNeighbour;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;

namespace CardGames.BeggarMyNeighbour.Compute
{
    /// <summary>
    /// Memetic algorithm: genetic algorithm where every individual undergoes a short
    /// hill-climb after crossover/mutation before being scored. Population stays near
    /// local optima, combining the global exploration of genetic search with the precision
    /// of local refinement.
    ///
    /// Compares against GeneticAlgorithm: local refinement per individual should reach
    /// higher scores faster at the cost of more compute per generation.
    /// </summary>
    public class MemeticAlgorithm : BeggarAlgorithm
    {
        private const int PopulationSize = 30;
        private const int LocalSearchSteps = 200;
        private const double BaseMutationRate = 0.2;
        private const double MaxMutationRate = 0.6;
        private const int StagnationLimit = 20;
        private const int ImmigrationInterval = 50;
        private const int ImmigrantCount = 5;

        public MemeticAlgorithm(ILogger logger, Random rng, int players, string user, string scoreboardUrl, string version, string instanceId, string team = null)
            : base(logger, rng, players, user, scoreboardUrl, version, instanceId, team) { }

        public override string Strategy => "memetic";

        protected override void DoRun(CancellationToken cancellationToken)
        {
            Logger.LogInformation("Memetic algorithm starting. Population={N}, LocalSearchSteps={S}", PopulationSize, LocalSearchSteps);

            int maxMoves = Math.Max(5000, Threshold * 3);

            // Seed initial population with scoreboard genomes where available.
            var seeds = FetchImmigrants();
            Logger.LogInformation("Seeding memetic population with {N} scoreboard genomes", seeds.Count);
            var population = seeds
                .Take(PopulationSize / 2)
                .Concat(Enumerable.Range(0, PopulationSize - Math.Min(seeds.Count, PopulationSize / 2))
                    .Select(_ => StructuredDeckUtils.RandomGenome(Rng)))
                .Take(PopulationSize)
                .ToList();

            // Locally refine every individual in the initial population.
            population = population.Select(g => LocalRefine(g, maxMoves, cancellationToken)).ToList();

            int generation = 0;
            int bestEver = 0;
            int stagnant = 0;
            double mutationRate = BaseMutationRate;

            while (!cancellationToken.IsCancellationRequested)
            {
                IncrementIteration();
                maxMoves = Math.Max(5000, Threshold * 3);

                var scored = population
                    .Select(g => (genome: g, score: StructuredDeckUtils.EvaluateBest(g, Players, maxMoves)))
                    .OrderByDescending(x => x.score)
                    .ToList();

                var best = scored[0];

                if (best.score > Threshold)
                {
                    var deck = StructuredDeckUtils.BuildDeck(best.genome);
                    SubmitGame(deck, new Game(deck, Players).Play());
                }

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

                // Occasional immigration from scoreboard.
                if (generation % ImmigrationInterval == 0)
                {
                    var immigrants = FetchImmigrants();
                    if (immigrants.Count > 0)
                    {
                        Logger.LogInformation("Generation {Gen}: importing {N} immigrants", generation, immigrants.Count);
                        for (int i = 0; i < Math.Min(immigrants.Count, ImmigrantCount); i++)
                        {
                            var refined = LocalRefine(immigrants[i], maxMoves, cancellationToken);
                            scored[scored.Count - 1 - i] = (genome: refined, score: 0);
                        }
                    }
                }

                // Select top half as parents.
                var parents = scored.Take(PopulationSize / 2).Select(x => x.genome).ToList();

                // Build next generation: elites kept, rest via crossover + mutation + local refinement.
                var nextGen = new List<List<int>>(PopulationSize);

                foreach (var p in parents.Take(PopulationSize / 10))
                    nextGen.Add(p);

                while (nextGen.Count < PopulationSize && !cancellationToken.IsCancellationRequested)
                {
                    var p1 = parents[Rng.Next(parents.Count)];
                    var p2 = parents[Rng.Next(parents.Count)];
                    var child = StructuredDeckUtils.Crossover(Rng, p1, p2);
                    if (Rng.NextDouble() < mutationRate)
                        child = StructuredDeckUtils.Mutate(Rng, child);
                    nextGen.Add(LocalRefine(child, maxMoves, cancellationToken));
                }

                population = nextGen;
                generation++;

                if (generation % ImmigrationInterval == 0)
                    Logger.LogInformation("Memetic gen {Gen}, best: {Score}, mutation: {Rate:P0}, stagnant: {Stagnant}",
                        generation, best.score, mutationRate, stagnant);
            }
        }

        private List<int> LocalRefine(List<int> genome, int maxMoves, CancellationToken ct)
        {
            var current = genome;
            int currentScore = StructuredDeckUtils.EvaluateBest(current, Players, maxMoves);

            for (int step = 0; step < LocalSearchSteps && !ct.IsCancellationRequested; step++)
            {
                var candidate = StructuredDeckUtils.Mutate(Rng, current);
                int candidateScore = StructuredDeckUtils.EvaluateBest(candidate, Players, maxMoves);
                if (candidateScore > currentScore)
                {
                    current = candidate;
                    currentScore = candidateScore;
                }
            }

            return current;
        }

        private List<List<int>> FetchImmigrants()
        {
            try
            {
                using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
                var json = client.GetStringAsync(ScoreboardUrl).Result;
                var array = JArray.Parse(json);
                return array
                    .Take(20)
                    .Select(item => item["Deck"]?.ToObject<List<int>>())
                    .Where(deck => deck != null && deck.Count == 52)
                    .Select(deck => StructuredDeckUtils.DeckToGenome(deck!))
                    .Where(g => g != null)
                    .Take(ImmigrantCount)
                    .ToList()!;
            }
            catch (Exception ex)
            {
                Logger.LogDebug(ex, "Could not fetch immigrants from scoreboard");
                return new List<List<int>>();
            }
        }
    }
}
