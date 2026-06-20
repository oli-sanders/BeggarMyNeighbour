using System;
using System.Collections.Generic;
using System.Linq;
using CardGames.BeggarMyNeighbour;
using Microsoft.Extensions.Logging;

namespace CardGames.BeggarMyNeighbour.Compute
{
    public class HillClimbAlgorithm : BeggarAlgorithm
    {
        public HillClimbAlgorithm(ILogger logger, Random rng, int players, string user, string scoreboardUrl, string version, string instanceId)
            : base(logger, rng, players, user, scoreboardUrl, version, instanceId) { }

        public override string Strategy => "hill-climb";

        public void Run()
        {
            Logger.LogInformation("Hill-climb starting");

            var deck = CardUtils.Shuffle(Rng, CardUtils.Deck).ToList();
            int currentScore = new Game(deck, Players).Play();

            while (true)
            {
                var candidate = Mutate(deck);
                int candidateScore = new Game(candidate, Players).Play();

                if (candidateScore >= currentScore)
                {
                    deck = candidate;
                    currentScore = candidateScore;
                }

                if (currentScore > Threshold)
                    SubmitGame(deck, currentScore);
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
