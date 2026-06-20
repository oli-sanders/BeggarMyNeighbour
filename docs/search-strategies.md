# Search Strategies for Finding Long Games

The current `Compute` worker uses a brute-force approach: Fisher-Yates shuffle a fresh 52-card deck,
play it, and submit if the score beats the current threshold. This document surveys alternative search
strategies and how each would fit into the existing `BeggarAlgorithm` / `BindBeggarAlgorithm`
architecture.

## Architecture Overview

Each strategy is implemented as a subclass of `BeggarAlgorithm`:

```csharp
public abstract class BeggarAlgorithm
{
    public abstract string Strategy { get; }   // reported to the scoreboard
    protected abstract IEnumerable<List<int>> Candidates(); // deck stream
}
```

The `Algorithm` environment variable controls which concrete class `Program.cs` instantiates.
The scoreboard `Strategy` field lets leaderboard entries be filtered and compared by algorithm.

The existing `GET /api/scores` endpoint returns full `Deck` arrays, which strategies can use to
seed local populations from globally discovered high-scoring decks.

---

## 1. Brute Force (current — `brute-force`)

**How it works**: Repeatedly Fisher-Yates shuffle a fresh 52-card deck and play it.

**Strengths**: Zero state; embarrassingly parallel; statistically unbiased coverage.

**Weaknesses**: No learning from good results. At scores above ~4 000 moves the probability of
random discovery drops sharply.

---

## 2. Hill Climbing (`hill-climb`)

**How it works**:
1. Start from a deck that already beats the current threshold (found by brute force or fetched from
   the scoreboard).
2. Repeatedly apply a small mutation — swap two randomly chosen cards.
3. Keep the mutated deck only if its score is ≥ the current best.

**Implementation sketch**:

```csharp
public class HillClimbAlgorithm : BeggarAlgorithm
{
    public override string Strategy => "hill-climb";

    protected override IEnumerable<List<int>> Candidates()
    {
        var deck = SeedFromScoreboard() ?? CardUtils.Shuffle(_rng);
        while (true)
        {
            var candidate = Mutate(deck);
            if (Game.Play(candidate) >= Game.Play(deck))
                deck = candidate;
            yield return candidate;
        }
    }

    private List<int> Mutate(List<int> deck)
    {
        var next = new List<int>(deck);
        int i = _rng.Next(next.Count), j = _rng.Next(next.Count);
        (next[i], next[j]) = (next[j], next[i]);
        return next;
    }
}
```

**Strengths**: Immediately exploits known-good decks; very cheap per iteration.

**Weaknesses**: Gets stuck in local maxima quickly — the score landscape is highly irregular.

---

## 3. Simulated Annealing (`simulated-annealing`)

**How it works**: Like hill climbing but accepts worse results with probability
`exp(-Δscore / T)`, where temperature `T` starts high (broad exploration) and cools over time.
At `T → 0` it converges to pure hill climbing.

**Key parameters**:

| Parameter | Suggested default |
|-----------|-------------------|
| Initial temperature | `500` |
| Cooling factor per step | `0.9999` |
| Minimum temperature | `1` |
| Reheat on new record | reset to `T_initial` |

**Implementation sketch**:

```csharp
public class SimulatedAnnealingAlgorithm : BeggarAlgorithm
{
    public override string Strategy => "simulated-annealing";

    protected override IEnumerable<List<int>> Candidates()
    {
        var deck = SeedFromScoreboard() ?? CardUtils.Shuffle(_rng);
        int currentScore = Game.Play(deck);
        double T = 500;

        while (true)
        {
            var candidate = Mutate(deck);
            int candidateScore = Game.Play(candidate);
            double delta = candidateScore - currentScore;

            if (delta >= 0 || _rng.NextDouble() < Math.Exp(delta / T))
            {
                deck = candidate;
                currentScore = candidateScore;
                if (delta > 0 && candidateScore > _globalBest) T = 500; // reheat
            }

            T = Math.Max(1, T * 0.9999);
            yield return deck;
        }
    }
}
```

**Strengths**: Escapes local maxima; well-understood and easy to tune; no inter-instance
communication needed (though reheating on a new global record via the scoreboard API helps).

**Weaknesses**: Sensitive to the cooling schedule; each process cools independently.

---

## 4. Genetic Algorithm (`genetic`)

**How it works**:
1. Maintain a population of `N` decks (e.g., 100).
2. Score all decks via `Game.Play()`.
3. Select the top 50% as parents.
4. Produce offspring via **order crossover (OX)**: preserves the relative order of cards from one
   parent while filling gaps from the other, guaranteeing a valid permutation.
5. Apply a low-rate swap mutation to each offspring.
6. Replace the bottom 50% with offspring.

**Island model**: The distributed `beggarcompute` instances naturally form an island-model GA.
Periodically each instance fetches top decks from `GET /api/scores` and injects them into its
local population as immigrants. The `Deck` field on `ScoreResponse` is already returned by the
API, so migration is essentially free.

**Strengths**: Maintains population diversity; crossover can combine complementary sub-sequences
from different decks; the existing multi-instance architecture maps directly onto islands.

**Weaknesses**: Order crossover for constrained permutations (all 52 cards exactly once) is
non-trivial to implement correctly. Requires careful tuning of population size, selection pressure,
and mutation rate.

---

## 5. Structural / Domain-Driven Search (`structured`)

**Insight**: The 16 picture cards (4×Ace, 4×King, 4×Queen, 4×Jack) drive all interesting game
behaviour. Their relative order determines chain reactions; the 36 number cards only pad the gaps.

**How it works**:
1. Enumerate promising orderings of the 16-element **picture-card subsequence** (or sample them,
   since 16! is still large).
2. For each picture-card ordering, randomly fill the remaining 36 positions with number cards and
   play several variants.
3. Apply hill climbing or annealing to the picture-card subsequence only.

**Strengths**: Reduces the effective search space dramatically; directly attacks the
game-mechanically significant part of the deck.

**Weaknesses**: Ignores the subtle effect of the number-card gap lengths between picture cards,
which influence when the pile is collected and re-entered into each hand.

---

## 6. Beam Search / Greedy Construction (`beam-search`)

**How it works**: Build the deck card by card. At each position, sample `k` candidate cards,
play the partial game as far as possible, and place the card that maximises the intermediate score.
Keep a beam of `B` partial decks in parallel.

**Strengths**: Directional; avoids wasting evaluations on clearly poor decks; parallelisable
across the beam width.

**Weaknesses**: The greedy lookahead is misleading — a card that appears poor at position 5 may
be critical at position 48. Beam width needs to be large to avoid premature convergence.

---

## 7. Monte Carlo Tree Search (`mcts`)

**How it works**: Model the deck as a sequence of card-placement decisions. Build a tree of
partial decks, expanding branches that tend to lead to high scores and using random rollouts
(complete random completions) to estimate branch value.

**Strengths**: Principled exploration/exploitation balance (UCT); could surface structural
insights (e.g., "Ace at position 13 consistently extends games").

**Weaknesses**: The raw branching factor (52 at the root) is too large without domain-specific
pruning. Recommended pruning: treat number cards as interchangeable at each step; MCTS only
decides *which picture card* goes where.

---

## Comparison Summary

| Strategy | Exploration | Exploitation | Implementation | Parallelism |
|----------|-------------|--------------|----------------|-------------|
| Brute force | ✅ excellent | ❌ none | trivial | embarrassingly parallel |
| Hill climbing | ❌ poor | ✅ excellent | simple | independent instances |
| Simulated annealing | ✅ good | ✅ good | simple | independent instances |
| Genetic algorithm | ✅ excellent | ✅ excellent | moderate | island model via scoreboard |
| Structured search | ✅ moderate | ✅ good | moderate | independent instances |
| Beam search | ❌ poor | ✅ good | moderate | beam-parallel |
| MCTS | ✅ excellent | ✅ excellent | complex | tree-parallel |

---

## Recommended Roadmap

1. **Short term**: Implement **simulated annealing**. Each instance brute-forces until it finds a
   deck above the threshold, then enters an annealing loop on that deck. Simple to add as a new
   `BeggarAlgorithm` subclass; controlled by `Algorithm=SimulatedAnnealing`.

2. **Medium term**: Implement the **genetic algorithm** with order crossover. Use the island model:
   each compute instance maintains a local population; periodically call `GET /api/scores` to fetch
   top decks and inject them as immigrants. The scoreboard leaderboard already stores full `Deck`
   arrays, making migration essentially free.

3. **Long term**: Explore **structured search** to understand which picture-card orderings are
   inherently long — this may yield theoretical insight into whether infinite games are possible.
