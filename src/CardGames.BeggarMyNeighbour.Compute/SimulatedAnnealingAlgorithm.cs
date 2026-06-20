using System;
using System.Collections.Generic;
using System.Threading;
using CardGames.BeggarMyNeighbour;
using Microsoft.Extensions.Logging;

namespace CardGames.BeggarMyNeighbour.Compute
{
    public class SimulatedAnnealingAlgorithm : BeggarAlgorithm
    {
        private const double InitialTemperature = 500.0;
        private const double CoolingFactor = 0.9999;
        private const double MinTemperature = 1.0;

        public SimulatedAnnealingAlgorithm(ILogger logger, Random rng, int players, string user, string scoreboardUrl, string version, string instanceId, string team = null)
            : base(logger, rng, players, user, scoreboardUrl, version, instanceId, team) { }

        public override string Strategy => "simulated-annealing";

        public void Run(CancellationToken cancellationToken = default)
        {
            Logger.LogInformation("Simulated annealing (structural) starting. T0={T}", InitialTemperature);

            var genome = StructuredDeckUtils.RandomGenome(Rng);
            int maxMoves = Math.Max(5000, Threshold * 3);
            int currentScore = StructuredDeckUtils.EvaluateBest(Rng, genome, Players, maxMoves: maxMoves);
            double T = InitialTemperature;

            while (!cancellationToken.IsCancellationRequested)
            {
                maxMoves = Math.Max(5000, Threshold * 3);
                var candidate = StructuredDeckUtils.Mutate(Rng, genome);
                int candidateScore = StructuredDeckUtils.EvaluateBest(Rng, candidate, Players, maxMoves: maxMoves);
                int delta = candidateScore - currentScore;

                if (delta >= 0 || Rng.NextDouble() < Math.Exp(delta / T))
                {
                    genome = candidate;
                    currentScore = candidateScore;

                    if (delta > 0 && currentScore > Threshold)
                    {
                        var deck = StructuredDeckUtils.BuildDeck(Rng, genome);
                        SubmitGame(deck, new Game(deck, Players).Play());
                        T = InitialTemperature; // reheat to keep exploring
                        continue;
                    }
                }

                T = Math.Max(MinTemperature, T * CoolingFactor);

                if (T <= MinTemperature)
                {
                    genome = StructuredDeckUtils.RandomGenome(Rng);
                    currentScore = StructuredDeckUtils.EvaluateBest(Rng, genome, Players, maxMoves: maxMoves);
                    T = InitialTemperature;
                    Logger.LogInformation("Annealing cycle complete, reseeding");
                }
            }
        }
    }
}
