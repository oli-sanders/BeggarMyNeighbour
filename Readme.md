# Beggar My Neighbour

A distributed system for finding long games of the card game *Beggar My Neighbour*. Multiple search workers run in Docker containers, each using a different algorithm to explore the deck space and submit high-scoring games to a shared scoreboard.

## Documentation

- [Architecture](docs/architecture.md) — services, data flow, verification pipeline, database schema
- [Search Algorithms](docs/algorithms.md) — genome representation, all algorithms, how to add a new one
- [API Reference](docs/api.md) — all endpoints with request/response schemas
- [Configuration](docs/configuration.md) — environment variables for every service

## The Game

Beggar My Neighbour is a deterministic card game: once the deck is shuffled, the outcome is fully determined by the card order. No decisions are made by players.

Rules:
1. Deal all cards face down. No player looks at or shuffles their hand.
2. Each player plays the top card from their hand face up onto a central pile until a picture card appears.
3. When a picture card is played, the next player must pay: 1 card for a Jack, 2 for a Queen, 3 for a King, 4 for an Ace.
4. If a picture card is played during a payment, the payment obligation passes to the next player.
5. If the payment completes without a picture card being played, the player who played the original picture card wins the pile and adds it to the bottom of their hand.
6. A player who plays their last card is eliminated. The last player remaining wins.

The key open question: can the game ever loop infinitely? This system searches for extremely long (potentially non-terminating) games.

## Technologies

- [.NET 8](https://dotnet.microsoft.com/) — ASP.NET Core Web API, Razor Pages, EF Core
- [SQLite](https://www.sqlite.org/) — scoreboard persistence
- [RabbitMQ](https://www.rabbitmq.com/) — async verification pipeline
- [Docker](https://www.docker.com/) / Docker Compose — containerised deployment

## Getting Started

### Prerequisites

- [Docker](https://www.docker.com/) and Docker Compose

### Run with Docker Compose

```sh
docker compose up --build
```

This starts RabbitMQ, the scoreboard API and web front end, the verifier, and all compute workers. Once healthy:

| Service | URL |
|---|---|
| Scoreboard (leaderboard) | http://localhost:8088 |
| Scoreboard API | http://localhost:8087/api/scores |
| RabbitMQ management UI | http://localhost:15672 |

Workers begin submitting scores within seconds. The leaderboard updates automatically.

### Build and run locally (without Docker)

```sh
dotnet build CardGames.BeggarMyNeighbour.sln

# Scoreboard API
dotnet run --project src/CardGames.BeggarMyNeighbour.Scoreboard.API

# Scoreboard web front end
dotnet run --project src/CardGames.BeggarMyNeighbour.Scoreboard.Web

# A compute worker
Algorithm=SimulatedAnnealing dotnet run --project src/CardGames.BeggarMyNeighbour.Compute
```

## Services

| Service | Description |
|---|---|
| **Compute** | Search workers. Each container runs one algorithm (set by `Algorithm` env var) and submits qualifying games to the scoreboard API. Multiple containers of the same image can run different algorithms simultaneously. |
| **Scoreboard API** | Receives game submissions, stores the top 1000 results per `(players, strategy)` bucket in SQLite, and queues games for verification via RabbitMQ. |
| **Scoreboard Web** | Razor Pages leaderboard showing the top games with filtering by strategy, user, and player count. Includes an improvement-over-time chart. |
| **Verify** | Replays submitted decks to confirm the reported score is accurate before the `IsVerified` flag is set. |

Full data flow: [architecture](docs/architecture.md).

## Algorithms

Seven search strategies are available, selected per-container via the `Algorithm` environment variable. Each reports scores under a distinct strategy name so results are tracked separately on the scoreboard.

| Algorithm | Strategy name | Approach |
|---|---|---|
| `BruteForce` | `brute-force` | Random genome each iteration |
| `SimulatedAnnealing` | `simulated-annealing` | Fixed cooling schedule |
| `SimulatedAnnealingAdaptive` | `simulated-annealing-adaptive` | Feedback-controlled temperature |
| `IteratedLocalSearch` | `iterated-local-search` | Perturb best + local hill-climb |
| `Memetic` | `memetic` | Genetic algorithm + per-individual local search |
| `TabuSearch` | `tabu-search` | Hill climb with tabu memory |
| `HillClimb` | `hill-climb` | Greedy hill climb with stagnation restart |
| `HillClimbParallel` | `hill-climb-parallel` | N parallel hill-climb chains |
| `Genetic` | `genetic` | Population-based genetic algorithm |

All algorithms use a 33-element **structural genome** (16 picture card positions + 17 gap sizes summing to 36) that makes deck evaluation fully deterministic. See [algorithms](docs/algorithms.md) for details on each strategy and how to add a new one.

## API

| Method | Route | Description |
|---|---|---|
| `POST` | `/api/scores` | Submit a game; returns current qualifying threshold |
| `GET` | `/api/scores` | Top 1000 games (filtered by `?players=N`) |
| `GET` | `/api/scores/threshold` | Qualifying threshold for `?players=N&strategy=X` |
| `GET` | `/api/scores/history` | Downsampled score history for charting |
| `GET` | `/health` | Health check |

Full request/response schemas: [api reference](docs/api.md).

## Configuration

Key environment variables for the compute worker:

| Variable | Description |
|---|---|
| `Algorithm` | Which algorithm to run (see table above) |
| `BeggarUser` | Name attributed to submitted scores |
| `BeggarTeam` | Optional team label |
| `BeggarPlayers` | Number of players (default `4`) |
| `ScoreboardUrl` | Full URL of the POST `/api/scores` endpoint |

Full configuration reference for all services: [configuration](docs/configuration.md).

## License

Released under the [MIT License](LICENSE.txt).
