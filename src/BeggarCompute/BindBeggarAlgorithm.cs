using System;
using System.Linq;
using BeggarMyNeighbour;
using Microsoft.Extensions.Logging;

namespace beggar
{
    public class BindBeggarAlgorithm : BeggarAlgorithm
    {

       public BindBeggarAlgorithm(ILogger logger, Random rng, int players, string user) : base(logger, rng, players, user)
        {

        }

       public void Run()
        {
            Logger.LogInformation("I'm Running");

            while (true)
            {
                var shuffleddeck = CardUtils.Shuffle(Rng, CardUtils.Deck);
                var ndgame = new Game(shuffleddeck.ToList(), Players);

                var result = ndgame.Play();

                //record long games
                if (result > Threshold)
                {
                    SubmitGame(shuffleddeck, result);
                }

            }
        }

    }

}