# Search Algorithms

## Genome Representation

All algorithms work on a **structural genome** — a compact, deterministic encoding of a 52-card deck that is faster to search than the raw deck permutation space.

A genome is a list of **33 integers**:

```
[p0 … p15 | g0 … g16]
 └── 16 ──┘ └── 17 ──┘
```

- **Positions 0–15** (`p`): the ordered sequence of picture cards.
  There are exactly 16 picture cards: four Jacks (value 1), four Queens (2), four Kings (3), four Aces (4).
- **Positions 16–32** (`g`): 17 gap sizes — the number of number cards (value 0) placed *before* each picture card and *after* the last one. The 17 gaps always sum to exactly 36 (the 36 non-picture cards).

`BuildDeck(genome)` is fully deterministic: the same genome always produces the same 52-card deck. This means:
- Found decks can be exactly reproduced from their genome.
- Genetic crossover and mutation operators work on a fixed-size structured representation.

### Mutation

Two equally-likely mutation operators:
- **Swap**: choose two positions in the picture section at random and swap them.
- **Shift**: move one number card from a non-empty gap to any other gap (preserves the sum-36 constraint).

### Crossover

Used by population-based algorithms (genetic, memetic):
- **Picture section**: Order Crossover (OX) — preserves relative order of picture cards from both parents.
- **Gap section**: uniform blend — each gap independently drawn from either parent, then normalised to sum to 36.

---

## Algorithms

All algorithms extend `BeggarAlgorithm` and are selected by the `Algorithm` environment variable. Each reports scores under a distinct **strategy name** which determines its threshold bucket on the scoreboard.

| `Algorithm` env value | Strategy name | Description |
|---|---|---|
| `BruteForce` | `brute-force` | Random genome per iteration — pure exploration |
| `SimulatedAnnealing` | `simulated-annealing` | Fixed cooling schedule |
| `SimulatedAnnealingAdaptive` | `simulated-annealing-adaptive` | Feedback-controlled temperature |
| `IteratedLocalSearch` | `iterated-local-search` | Perturb best + local hill-climb |
| `Memetic` | `memetic` | Genetic algorithm with per-individual local refinement |
| `TabuSearch` | `tabu-search` | Hill climb with tabu memory and perturbation restart |
| `HillClimb` | `hill-climb` | Greedy hill climb with stagnation restart |
| `HillClimbParallel` | `hill-climb-parallel` | N independent hill-climb chains in parallel |
| `Genetic` | `genetic` | Population-based genetic algorithm |

### Brute-force (`BruteForce`)

Generates a uniformly random genome each iteration and evaluates it. No memory of previous results. Acts as a baseline and surprisingly competitive at high volumes because the search space is large enough that random sampling finds interesting regions.

**Key parameters:** none — purely random.

### Simulated Annealing (`SimulatedAnnealing`)

Single-solution search. Accepts a mutated candidate with probability `exp(Δ/T)` when the candidate is worse. Temperature decays by a fixed `CoolingFactor = 0.9999` per step, then reheats after a qualifying submission or on reaching `MinTemperature`.

**Key parameters:**
- `InitialTemperature = 500`
- `CoolingFactor = 0.9999`
- `MinTemperature = 1.0`

### Adaptive Simulated Annealing (`SimulatedAnnealingAdaptive`)

Same acceptance criterion as SA but the temperature is both gently cooled each step (`DefaultCoolingFactor = 0.9999`) **and** corrected every 200 steps based on the measured acceptance rate. If acceptance is too high (> 25%) the temperature is reduced; too low (< 15%) it is raised. This makes the algorithm robust to changing score landscapes as the threshold rises.

**Key parameters:**
- `WindowSize = 200`
- `TargetAcceptRate = 0.20`
- `TempAdjustFactor = 1.1`

### Iterated Local Search (`IteratedLocalSearch`)

Maintains a **home base** genome (the best local optimum found so far). Each outer iteration:
1. Applies `PerturbationStrength` random mutations to the home base.
2. Runs a short hill-climb (`LocalSearchStagnationLimit = 2000` non-improving steps) from the perturbed point.
3. If the local-search result beats the home base, it becomes the new home base.
4. After `MaxFailedPerturbations = 30` consecutive failures, reseeds completely from random.

Compares to plain hill-climb: informed perturbation from the best-known solution preserves accumulated structure rather than discarding it on restart.

### Memetic (`Memetic`)

Genetic algorithm where every individual in the population undergoes `LocalSearchSteps = 200` hill-climb steps after crossover/mutation before fitness evaluation. The population therefore stays near local optima, giving the genetic operators something well-refined to cross over.

Immigration from the scoreboard top scores (every 50 generations) provides diversity from external sources.

**Key parameters:**
- `PopulationSize = 30`
- `LocalSearchSteps = 200`
- `BaseMutationRate = 0.2`, `MaxMutationRate = 0.6`
- `ImmigrationInterval = 50`

### Tabu Search (`TabuSearch`)

Single-solution search with explicit short-term memory. Each iteration evaluates `NeighborhoodSize = 50` candidate mutations and accepts the best one that is **not tabu** (recently visited). A tabu candidate is accepted anyway if it beats the all-time best (**aspiration criterion**).

After `RestartAfterStagnation = 300` non-improving iterations, applies `PerturbationStrength = 5` mutations to the **best-known genome** and restarts tabu search from there — preserving accumulated structure rather than starting cold.

**Key parameters:**
- `TabuTenure = 100` (entries in the tabu list)
- `NeighborhoodSize = 50`

### Hill Climb (`HillClimb`)

Greedy single-solution search. Accepts a mutant only if it is strictly better. Lateral moves (equal score) are accepted without resetting the stagnation counter. After `StagnationLimit = 10,000` non-improving steps, reseeds from a random genome.

### Hill Climb Parallel (`HillClimbParallel`)

Runs `ProcessorCount - 1` independent hill-climb chains simultaneously, each with its own `Random` instance. All chains share the common `SubmitGame` lock in the base class so personal-best tracking remains correct under concurrent access. Useful for saturating a multi-core machine with a single container.

### Genetic (`Genetic`)

Standard genetic algorithm with population size 100, OX crossover, and adaptive mutation (rate rises toward 50% as stagnation increases). Imports top scoreboard genomes as immigrants every 100 generations. Parallelised fitness evaluation using PLINQ.

---

## Base Class: `BeggarAlgorithm`

All algorithms inherit from `BeggarAlgorithm`, which provides:

- **`FetchThreshold()`** — called once on startup, reads the per-`(players, strategy)` threshold from the scoreboard so the algorithm knows the current qualifying bar before it has submitted anything.
- **`SubmitGame(deck, length)`** — thread-safe gate that only submits if `length > bestSubmitted`. Calls the scoreboard API, updates `_threshold` from the response, and increments `_bestSubmitted`. All algorithms benefit from this without any per-algorithm bookkeeping.
- **`Threshold`** property — the last threshold received from the scoreboard.
- **`IncrementIteration()`** — thread-safe iteration counter, reported with every submission.

---

## Adding a New Algorithm

1. Create a class in `src/CardGames.BeggarMyNeighbour.Compute/` extending `BeggarAlgorithm`.
2. Override `Strategy` to return a unique string — this becomes the scoreboard bucket name.
3. Override `DoRun(CancellationToken)` with the search loop. Call `IncrementIteration()` at the top of the inner loop and `SubmitGame(deck, score)` when a candidate exceeds `Threshold`.
4. Add a `case "YourAlgorithmName":` to the `switch` in `Program.cs`.
5. Add a service entry in `docker-compose.yml` using `<<: *compute-base` and `Algorithm=YourAlgorithmName`.
