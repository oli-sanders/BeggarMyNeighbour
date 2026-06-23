using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;

namespace CardGames.BeggarMyNeighbour.Compute
{
    public class MemeticHeuristicAlgorithm : MemeticAlgorithm
    {
        public MemeticHeuristicAlgorithm(ILogger logger, Random rng, int players, string user, string scoreboardUrl, string version, string instanceId, string team = null)
            : base(logger, rng, players, user, scoreboardUrl, version, instanceId, team) { }

        public override string Strategy => "memetic-heuristic";

        protected override List<int> ApplyMutation(Random rng, List<int> genome) =>
            StructuredDeckUtils.HeuristicMutate(rng, genome);
    }
}
