using System;
using System.Collections.Generic;
using CardGames.BeggarMyNeighbour;
using Microsoft.Extensions.Logging;

namespace CardGames.BeggarMyNeighbour.Compute
{
    public class HillClimbAlgorithm : BeggarAlgorithm
    {
        public HillClimbAlgorithm(ILogger logger, Random rng, int players, string user, string scoreboardUrl, string version, string instanceId, string team = null)
            : base(logger, rng, players, user, scoreboardUrl, version, instanceId, team) { }

        public override string Strategy => "hill-climb";

        public void Run()
        {
            Logger.LogInformation("Hill-climb (structural) starting");

            var genome = StructuredDeckUtils.RandomGenome(Rng);
            int currentScore = StructuredDeckUtils.EvaluateBest(Rng, genome, Players);

            while (true)
            {
                var candidate = StructuredDeckUtils.Mutate(Rng, genome);
                int candidateScore = StructuredDeckUtils.EvaluateBest(Rng, candidate, Players);

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
