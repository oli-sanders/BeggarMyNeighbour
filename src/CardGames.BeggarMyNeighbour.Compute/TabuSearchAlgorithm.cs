using System;
using System.Collections.Generic;
using System.Threading;
using CardGames.BeggarMyNeighbour;
using Microsoft.Extensions.Logging;

namespace CardGames.BeggarMyNeighbour.Compute
{
    /// <summary>
    /// Tabu search: hill climb with a fixed-size tabu list of recently visited genome
    /// hashes. At each step, NeighborhoodSize candidates are evaluated and the best
    /// non-tabu one is accepted (even if it is worse than the current). A tabu move
    /// is accepted anyway if it beats the all-time best (aspiration criterion).
    ///
    /// Compares against HillClimbAlgorithm: explicit memory of visited states prevents
    /// cycling and allows the search to escape local optima without a full restart.
    /// </summary>
    public class TabuSearchAlgorithm : BeggarAlgorithm
    {
        private const int TabuTenure = 100;
        private const int NeighborhoodSize = 50;
        private const int RestartAfterStagnation = 300;
        private const int PerturbationStrength = 5;

        public TabuSearchAlgorithm(ILogger logger, Random rng, int players, string user, string scoreboardUrl, string version, string instanceId, string team = null)
            : base(logger, rng, players, user, scoreboardUrl, version, instanceId, team) { }

        public override string Strategy => "tabu-search";

        protected override void DoRun(CancellationToken cancellationToken)
        {
            Logger.LogInformation("Tabu search starting. Tenure={T}, Neighborhood={N}", TabuTenure, NeighborhoodSize);

            var genome = StructuredDeckUtils.RandomGenome(Rng);
            int maxMoves = Math.Max(5000, Threshold * 3);
            int currentScore = StructuredDeckUtils.EvaluateBest(genome, Players, maxMoves);

            var tabuQueue = new Queue<int>(TabuTenure + 1);
            var tabuSet = new HashSet<int>();

            int bestScore = currentScore;
            var bestGenome = genome;
            int stagnant = 0;

            while (!cancellationToken.IsCancellationRequested)
            {
                IncrementIteration();
                maxMoves = Math.Max(5000, Threshold * 3);

                // Evaluate neighbourhood and pick the best non-tabu candidate.
                // A tabu candidate is still accepted if it beats the all-time best (aspiration).
                List<int> bestNeighbour = null;
                int bestNeighbourScore = int.MinValue;

                for (int n = 0; n < NeighborhoodSize; n++)
                {
                    var candidate = ApplyMutation(Rng, genome);
                    int candidateScore = StructuredDeckUtils.EvaluateBest(candidate, Players, maxMoves);
                    int hash = GetGenomeHash(candidate);
                    bool isTabu = tabuSet.Contains(hash);

                    if ((!isTabu || candidateScore > bestScore) && candidateScore > bestNeighbourScore)
                    {
                        bestNeighbourScore = candidateScore;
                        bestNeighbour = candidate;
                    }
                }

                if (bestNeighbour != null)
                {
                    genome = bestNeighbour;
                    currentScore = bestNeighbourScore;

                    int hash = GetGenomeHash(genome);
                    if (!tabuSet.Contains(hash))
                    {
                        tabuQueue.Enqueue(hash);
                        tabuSet.Add(hash);
                        if (tabuQueue.Count > TabuTenure)
                            tabuSet.Remove(tabuQueue.Dequeue());
                    }

                    if (currentScore > bestScore)
                    {
                        bestScore = currentScore;
                        bestGenome = genome;
                        stagnant = 0;
                    }
                    else
                    {
                        stagnant++;
                    }
                }
                else
                {
                    stagnant++;
                }

                if (bestScore > Threshold)
                {
                    var deck = StructuredDeckUtils.BuildDeck(bestGenome);
                    SubmitGame(deck, new Game(deck, Players).Play());
                }

                if (stagnant >= RestartAfterStagnation)
                {
                    // Perturb from best-known rather than a cold random restart — keeps us in a
                    // promising region of the search space while escaping the current local optimum.
                    var perturbed = new List<int>(bestGenome);
                    for (int p = 0; p < PerturbationStrength; p++)
                        perturbed = StructuredDeckUtils.Mutate(Rng, perturbed);
                    genome = perturbed;
                    currentScore = StructuredDeckUtils.EvaluateBest(genome, Players, maxMoves);
                    tabuQueue.Clear();
                    tabuSet.Clear();
                    stagnant = 0;
                    Logger.LogInformation("Tabu search: perturbation restart from best ({Score})", bestScore);
                }
            }
        }

        private static int GetGenomeHash(List<int> genome)
        {
            unchecked
            {
                int hash = 17;
                foreach (var v in genome)
                    hash = hash * 31 + v;
                return hash;
            }
        }
    }
}
