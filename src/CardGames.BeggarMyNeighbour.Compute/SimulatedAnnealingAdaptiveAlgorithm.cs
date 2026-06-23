using System;
using System.Threading;
using CardGames.BeggarMyNeighbour;
using Microsoft.Extensions.Logging;

namespace CardGames.BeggarMyNeighbour.Compute
{
    /// <summary>
    /// Simulated annealing with feedback-controlled temperature: every WindowSize iterations
    /// the acceptance rate is measured and temperature is adjusted up or down to maintain
    /// the target acceptance rate. Avoids manual tuning of the cooling schedule.
    ///
    /// Compares against SimulatedAnnealingAlgorithm: adaptive schedule should be more robust
    /// as the score landscape changes (e.g. after the threshold shifts upward).
    /// </summary>
    public class SimulatedAnnealingAdaptiveAlgorithm : BeggarAlgorithm
    {
        private const int WindowSize = 200;
        private const double TargetAcceptRate = 0.20;
        private const double TempAdjustFactor = 1.1;
        private const double DefaultCoolingFactor = 0.9999;
        private const double InitialTemperature = 100.0;
        private const double MinTemperature = 0.01;
        private const double MaxTemperature = 100_000.0;

        public SimulatedAnnealingAdaptiveAlgorithm(ILogger logger, Random rng, int players, string user, string scoreboardUrl, string version, string instanceId, string team = null)
            : base(logger, rng, players, user, scoreboardUrl, version, instanceId, team) { }

        public override string Strategy => "simulated-annealing-adaptive";

        protected override void DoRun(CancellationToken cancellationToken)
        {
            Logger.LogInformation("Adaptive SA starting. Target accept rate={R:P0}, window={W}", TargetAcceptRate, WindowSize);

            var genome = StructuredDeckUtils.RandomGenome(Rng);
            int maxMoves = Math.Max(5000, Threshold * 3);
            int currentScore = StructuredDeckUtils.EvaluateBest(genome, Players, maxMoves);
            double T = InitialTemperature;
            int acceptedInWindow = 0;
            int windowCount = 0;

            while (!cancellationToken.IsCancellationRequested)
            {
                IncrementIteration();
                maxMoves = Math.Max(5000, Threshold * 3);

                var candidate = ApplyMutation(Rng, genome);
                int candidateScore = StructuredDeckUtils.EvaluateBest(candidate, Players, maxMoves);
                int delta = candidateScore - currentScore;

                bool accepted = delta >= 0 || Rng.NextDouble() < Math.Exp(delta / T);
                if (accepted)
                {
                    genome = candidate;
                    currentScore = candidateScore;
                    acceptedInWindow++;
                }
                windowCount++;

                // Per-step cooling keeps the search converging; window-based feedback corrects large deviations.
                T = Math.Max(MinTemperature, T * DefaultCoolingFactor);

                if (windowCount >= WindowSize)
                {
                    double rate = (double)acceptedInWindow / windowCount;
                    if (rate > TargetAcceptRate + 0.05)
                        T = Math.Max(MinTemperature, T / TempAdjustFactor);
                    else if (rate < TargetAcceptRate - 0.05)
                        T = Math.Min(MaxTemperature, T * TempAdjustFactor);

                    Logger.LogInformation("Adaptive SA: T={T:F3}, accept={Rate:P0}", T, rate);
                    acceptedInWindow = 0;
                    windowCount = 0;
                }

                if (currentScore > Threshold)
                {
                    var deck = StructuredDeckUtils.BuildDeck(genome);
                    SubmitGame(deck, new Game(deck, Players).Play());
                    T = Math.Max(T, InitialTemperature);
                    acceptedInWindow = 0;
                    windowCount = 0;
                }

                // Reseed if temperature collapsed (all moves rejected for too long).
                if (T <= MinTemperature)
                {
                    Logger.LogInformation("Adaptive SA: temperature collapsed, reseeding");
                    genome = StructuredDeckUtils.RandomGenome(Rng);
                    currentScore = StructuredDeckUtils.EvaluateBest(genome, Players, maxMoves);
                    T = InitialTemperature;
                    acceptedInWindow = 0;
                    windowCount = 0;
                }
            }
        }
    }
}
