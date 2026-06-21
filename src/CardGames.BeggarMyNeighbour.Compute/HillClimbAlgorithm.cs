using System;
using System.Collections.Generic;
using System.Threading;
using CardGames.BeggarMyNeighbour;
using Microsoft.Extensions.Logging;

namespace CardGames.BeggarMyNeighbour.Compute
{
    public class HillClimbAlgorithm : BeggarAlgorithm
    {
        private const int StagnationLimit = 10_000;

        public HillClimbAlgorithm(ILogger logger, Random rng, int players, string user, string scoreboardUrl, string version, string instanceId, string team = null)
            : base(logger, rng, players, user, scoreboardUrl, version, instanceId, team) { }

        public override string Strategy => "hill-climb";

        protected override void DoRun(CancellationToken cancellationToken)
        {
            Logger.LogInformation("Hill-climb (structural) starting");

            var genome = StructuredDeckUtils.RandomGenome(Rng);
            int maxMoves = Math.Max(5000, Threshold * 3);
            int currentScore = StructuredDeckUtils.EvaluateBest(genome, Players, maxMoves: maxMoves);
            int stagnation = 0;

            while (!cancellationToken.IsCancellationRequested)
            {
                IncrementIteration();
                maxMoves = Math.Max(5000, Threshold * 3);
                var candidate = StructuredDeckUtils.Mutate(Rng, genome);
                int candidateScore = StructuredDeckUtils.EvaluateBest(candidate, Players, maxMoves: maxMoves);

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
                    genome = candidate; // accept lateral moves without re-submitting
                    stagnation++;
                }
                else
                {
                    stagnation++;
                }

                if (stagnation >= StagnationLimit)
                {
                    Logger.LogInformation("Hill-climb stagnated after {N} iterations, reseeding", stagnation);
                    genome = StructuredDeckUtils.RandomGenome(Rng);
                    currentScore = StructuredDeckUtils.EvaluateBest(genome, Players, maxMoves: maxMoves);
                    stagnation = 0;
                }
            }
        }
    }
}
