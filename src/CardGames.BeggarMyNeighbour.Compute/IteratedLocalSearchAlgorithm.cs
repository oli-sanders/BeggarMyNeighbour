using System;
using System.Collections.Generic;
using System.Threading;
using CardGames.BeggarMyNeighbour;
using Microsoft.Extensions.Logging;

namespace CardGames.BeggarMyNeighbour.Compute
{
    /// <summary>
    /// Iterated Local Search: repeatedly perturbs the best-known genome and applies
    /// a short hill-climb from the perturbed point. Keeps the best overall result as
    /// the "home base" for subsequent perturbations.
    ///
    /// Compares against HillClimbAlgorithm: smarter restart strategy (informed perturbation
    /// vs. full random reseed) should escape local optima while retaining accumulated structure.
    /// </summary>
    public class IteratedLocalSearchAlgorithm : BeggarAlgorithm
    {
        private const int LocalSearchStagnationLimit = 2_000;
        private const int PerturbationStrength = 4;
        private const int MaxFailedPerturbations = 30;

        public IteratedLocalSearchAlgorithm(ILogger logger, Random rng, int players, string user, string scoreboardUrl, string version, string instanceId, string team = null)
            : base(logger, rng, players, user, scoreboardUrl, version, instanceId, team) { }

        public override string Strategy => "iterated-local-search";

        protected override void DoRun(CancellationToken cancellationToken)
        {
            Logger.LogInformation("Iterated local search starting. LocalStagnation={S}, Perturbation={P}", LocalSearchStagnationLimit, PerturbationStrength);

            int maxMoves = Math.Max(5000, Threshold * 3);
            var bestGenome = StructuredDeckUtils.RandomGenome(Rng);
            int bestScore = StructuredDeckUtils.EvaluateBest(bestGenome, Players, maxMoves);

            // Initial local search from the random start.
            (bestGenome, bestScore) = LocalSearch(bestGenome, maxMoves, cancellationToken);

            int failedPerturbations = 0;
            int ilsIteration = 0;

            while (!cancellationToken.IsCancellationRequested)
            {
                maxMoves = Math.Max(5000, Threshold * 3);

                // Perturbation: apply PerturbationStrength mutations to the home base.
                var perturbed = new List<int>(bestGenome);
                for (int p = 0; p < PerturbationStrength; p++)
                    perturbed = StructuredDeckUtils.Mutate(Rng, perturbed);

                // Local search from perturbed point.
                var (localGenome, localScore) = LocalSearch(perturbed, maxMoves, cancellationToken);

                if (localScore >= bestScore)
                {
                    bestGenome = localGenome;
                    bestScore = localScore;
                    failedPerturbations = 0;
                }
                else
                {
                    failedPerturbations++;
                }

                if (bestScore > Threshold)
                {
                    var deck = StructuredDeckUtils.BuildDeck(bestGenome);
                    SubmitGame(deck, new Game(deck, Players).Play());
                }

                // Full reseed after too many failed perturbations.
                if (failedPerturbations >= MaxFailedPerturbations)
                {
                    Logger.LogInformation("ILS reseeding after {N} failed perturbations (best so far: {Score})", failedPerturbations, bestScore);
                    maxMoves = Math.Max(5000, Threshold * 3);
                    bestGenome = StructuredDeckUtils.RandomGenome(Rng);
                    bestScore = StructuredDeckUtils.EvaluateBest(bestGenome, Players, maxMoves);
                    (bestGenome, bestScore) = LocalSearch(bestGenome, maxMoves, cancellationToken);
                    failedPerturbations = 0;
                }

                ilsIteration++;
                if (ilsIteration % 50 == 0)
                    Logger.LogInformation("ILS iteration {N}, best local optimum: {Score}", ilsIteration, bestScore);
            }
        }

        private (List<int> genome, int score) LocalSearch(List<int> startGenome, int maxMoves, CancellationToken ct)
        {
            var genome = new List<int>(startGenome);
            int currentScore = StructuredDeckUtils.EvaluateBest(genome, Players, maxMoves);
            int stagnation = 0;

            while (stagnation < LocalSearchStagnationLimit && !ct.IsCancellationRequested)
            {
                IncrementIteration();
                var candidate = ApplyMutation(Rng, genome);
                int candidateScore = StructuredDeckUtils.EvaluateBest(candidate, Players, maxMoves);

                if (candidateScore >= currentScore)
                {
                    stagnation = (candidateScore > currentScore) ? 0 : stagnation + 1;
                    genome = candidate;
                    currentScore = candidateScore;
                }
                else
                {
                    stagnation++;
                }
            }

            return (genome, currentScore);
        }
    }
}
