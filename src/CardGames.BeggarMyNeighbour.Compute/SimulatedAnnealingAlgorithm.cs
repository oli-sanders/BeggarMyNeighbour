using System;
using System.Collections.Generic;
using System.Linq;
using CardGames.BeggarMyNeighbour;
using Microsoft.Extensions.Logging;

namespace CardGames.BeggarMyNeighbour.Compute
{
    public class SimulatedAnnealingAlgorithm : BeggarAlgorithm
    {
        private const double InitialTemperature = 500.0;
        private const double CoolingFactor = 0.9999;
        private const double MinTemperature = 1.0;

        public SimulatedAnnealingAlgorithm(ILogger logger, Random rng, int players, string user, string scoreboardUrl, string version, string instanceId)
            : base(logger, rng, players, user, scoreboardUrl, version, instanceId) { }

        public override string Strategy => "simulated-annealing";

        public void Run()
        {
            Logger.LogInformation("Simulated annealing starting. T0={T}", InitialTemperature);

            var deck = CardUtils.Shuffle(Rng, CardUtils.Deck).ToList();
            int currentScore = new Game(deck, Players).Play();
            double T = InitialTemperature;

            while (true)
            {
                var candidate = Mutate(deck);
                int candidateScore = new Game(candidate, Players).Play();
                int delta = candidateScore - currentScore;

                if (delta >= 0 || Rng.NextDouble() < Math.Exp(delta / T))
                {
                    deck = candidate;
                    currentScore = candidateScore;

                    if (delta > 0 && currentScore > Threshold)
                    {
                        SubmitGame(deck, currentScore);
                        // reheat so we keep exploring from this good region
                        T = InitialTemperature;
                        continue;
                    }
                }

                T = Math.Max(MinTemperature, T * CoolingFactor);

                // When fully cooled, reseed from a fresh random deck
                if (T <= MinTemperature)
                {
                    deck = CardUtils.Shuffle(Rng, CardUtils.Deck).ToList();
                    currentScore = new Game(deck, Players).Play();
                    T = InitialTemperature;
                    Logger.LogInformation("Annealing cycle complete, reseeding");
                }
            }
        }

        private List<int> Mutate(List<int> deck)
        {
            var next = new List<int>(deck);
            int i = Rng.Next(next.Count);
            int j = Rng.Next(next.Count);
            (next[i], next[j]) = (next[j], next[i]);
            return next;
        }
    }
}
