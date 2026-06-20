using System;
using System.Collections.Generic;
using System.Threading;
using CardGames.BeggarMyNeighbour;
using Microsoft.Extensions.Logging;

namespace CardGames.BeggarMyNeighbour.Compute
{
    public class HillClimbAlgorithm : BeggarAlgorithm
    {
        public HillClimbAlgorithm(ILogger logger, Random rng, int players, string user, string scoreboardUrl, string version, string instanceId, string team = null)
            : base(logger, rng, players, user, scoreboardUrl, version, instanceId, team) { }

        public override string Strategy => "hill-climb";

        public void Run(CancellationToken cancellationToken = default)
        {
            Logger.LogInformation("Hill-climb (structural) starting");

            var genome = StructuredDeckUtils.RandomGenome(Rng);
            int maxMoves = Math.Max(5000, Threshold * 3);
            int currentScore = StructuredDeckUtils.EvaluateBest(Rng, genome, Players, maxMoves: maxMoves);

            while (!cancellationToken.IsCancellationRequested)
            {
                maxMoves = Math.Max(5000, Threshold * 3);
                var candidate = StructuredDeckUtils.Mutate(Rng, genome);
                int candidateScore = StructuredDeckUtils.EvaluateBest(Rng, candidate, Players, maxMoves: maxMoves);

                if (candidateScore >= currentScore)
                {
                    genome = candidate;
                    currentScore = candidateScore;
                }

                if (currentScore > Threshold)
                {
                    var deck = StructuredDeckUtils.BuildDeck(Rng, genome);
                    SubmitGame(deck, new Game(deck, Players).Play());
                }
            }
        }
    }
}
