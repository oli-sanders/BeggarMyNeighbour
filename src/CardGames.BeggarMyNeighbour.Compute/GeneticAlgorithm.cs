using System;
using System.Collections.Generic;
using System.Linq;
using CardGames.BeggarMyNeighbour;
using Microsoft.Extensions.Logging;

namespace CardGames.BeggarMyNeighbour.Compute
{
    public class GeneticAlgorithm : BeggarAlgorithm
    {
        private const int PopulationSize = 100;
        private const double MutationRate = 0.1;
        private const int ImmigrationInterval = 500; // generations between scoreboard imports

        public GeneticAlgorithm(ILogger logger, Random rng, int players, string user, string scoreboardUrl, string version, string instanceId)
            : base(logger, rng, players, user, scoreboardUrl, version, instanceId) { }

        public override string Strategy => "genetic";

        public void Run()
        {
            Logger.LogInformation("Genetic algorithm (structural) starting. Population={N}", PopulationSize);

            // Initialise random population of picture-card genomes
            var population = Enumerable.Range(0, PopulationSize)
                .Select(_ => StructuredDeckUtils.RandomGenome(Rng))
                .ToList();

            int generation = 0;

            while (true)
            {
                // Evaluate
                var scored = population
                    .Select(g => (genome: g, score: StructuredDeckUtils.EvaluateBest(Rng, g, Players)))
                    .OrderByDescending(x => x.score)
                    .ToList();

                var best = scored[0];
                if (best.score > Threshold)
                {
                    var deck = StructuredDeckUtils.BuildDeck(Rng, best.genome);
                    SubmitGame(deck, new Game(deck, Players).Play());
                }

                // Select top half as parents
                var parents = scored.Take(PopulationSize / 2).Select(x => x.genome).ToList();

                // Produce next generation
                var nextGen = new List<List<int>>(PopulationSize);

                // Elitism: keep top 10%
                foreach (var p in parents.Take(PopulationSize / 10))
                    nextGen.Add(p);

                // Fill rest with crossover + mutation
                while (nextGen.Count < PopulationSize)
                {
                    var p1 = parents[Rng.Next(parents.Count)];
                    var p2 = parents[Rng.Next(parents.Count)];
                    var child = StructuredDeckUtils.Crossover(Rng, p1, p2);
                    if (Rng.NextDouble() < MutationRate)
                        child = StructuredDeckUtils.Mutate(Rng, child);
                    nextGen.Add(child);
                }

                population = nextGen;
                generation++;

                if (generation % ImmigrationInterval == 0)
                    Logger.LogInformation("Generation {Gen}, best this cycle: {Score}", generation, best.score);
            }
        }
    }
}
