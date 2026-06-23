using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;

namespace CardGames.BeggarMyNeighbour.Compute
{
    /// <summary>
    /// Iterated Local Search using heuristic mutations derived from pattern analysis of
    /// top-scoring decks: Ace-avoidance, K→Q promotion, and gap redistribution toward
    /// the middle third. Compares against IteratedLocalSearchAlgorithm to measure the
    /// value of domain-informed mutation operators.
    /// </summary>
    public class IteratedLocalSearchHeuristicAlgorithm : IteratedLocalSearchAlgorithm
    {
        public IteratedLocalSearchHeuristicAlgorithm(ILogger logger, Random rng, int players, string user, string scoreboardUrl, string version, string instanceId, string team = null)
            : base(logger, rng, players, user, scoreboardUrl, version, instanceId, team) { }

        public override string Strategy => "iterated-local-search-heuristic";

        protected override List<int> ApplyMutation(Random rng, List<int> genome) =>
            StructuredDeckUtils.HeuristicMutate(rng, genome);
    }
}
