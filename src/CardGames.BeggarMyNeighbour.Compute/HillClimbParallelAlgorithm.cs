using System;
using System.Threading;
using CardGames.BeggarMyNeighbour;
using Microsoft.Extensions.Logging;

namespace CardGames.BeggarMyNeighbour.Compute
{
    /// <summary>
    /// Runs N independent hill-climb chains in parallel (one per core), all sharing the
    /// global best-submitted guard in the base class. Compares against HillClimbAlgorithm
    /// to quantify the benefit of more restarts per unit time.
    /// </summary>
    public class HillClimbParallelAlgorithm : BeggarAlgorithm
    {
        private const int StagnationLimit = 10_000;

        public HillClimbParallelAlgorithm(ILogger logger, Random rng, int players, string user, string scoreboardUrl, string version, string instanceId, string team = null)
            : base(logger, rng, players, user, scoreboardUrl, version, instanceId, team) { }

        public override string Strategy => "hill-climb-parallel";

        protected override void DoRun(CancellationToken cancellationToken)
        {
            int threadCount = Math.Max(2, Environment.ProcessorCount - 1);
            Logger.LogInformation("Parallel hill-climb starting on {N} threads", threadCount);

            // Create per-thread Randoms before spawning — Rng is not thread-safe.
            var threadRngs = new Random[threadCount];
            for (int i = 0; i < threadCount; i++)
                threadRngs[i] = new Random(Rng.Next());

            var threads = new Thread[threadCount];
            for (int i = 0; i < threadCount; i++)
            {
                var rng = threadRngs[i];
                int threadId = i;
                threads[i] = new Thread(() =>
                {
                    var genome = StructuredDeckUtils.RandomGenome(rng);
                    int maxMoves = Math.Max(5000, Threshold * 3);
                    int currentScore = StructuredDeckUtils.EvaluateBest(genome, Players, maxMoves);
                    int stagnation = 0;

                    while (!cancellationToken.IsCancellationRequested)
                    {
                        IncrementIteration();
                        maxMoves = Math.Max(5000, Threshold * 3);
                        var candidate = StructuredDeckUtils.Mutate(rng, genome);
                        int candidateScore = StructuredDeckUtils.EvaluateBest(candidate, Players, maxMoves);

                        if (candidateScore > currentScore)
                        {
                            genome = candidate;
                            currentScore = candidateScore;
                            stagnation = 0;
                            if (currentScore > Threshold)
                            {
                                var deck = StructuredDeckUtils.BuildDeck(genome);
                                SubmitGame(deck, new Game(deck, Players).Play());
                            }
                        }
                        else if (candidateScore == currentScore)
                        {
                            genome = candidate;
                            stagnation++;
                        }
                        else
                        {
                            stagnation++;
                        }

                        if (stagnation >= StagnationLimit)
                        {
                            Logger.LogDebug("Thread {Id} reseeding after {N} stagnant iterations", threadId, stagnation);
                            genome = StructuredDeckUtils.RandomGenome(rng);
                            currentScore = StructuredDeckUtils.EvaluateBest(genome, Players, maxMoves);
                            stagnation = 0;
                        }
                    }
                }) { IsBackground = true, Name = $"hill-climb-{i}" };
            }

            foreach (var t in threads) t.Start();
            foreach (var t in threads) t.Join();
        }
    }
}
