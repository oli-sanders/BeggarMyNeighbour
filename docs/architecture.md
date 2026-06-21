# System Architecture

## Services

| Service | Docker image | Port (host) | Purpose |
|---|---|---|---|
| `scoreboard.api` | `beggar-scoreboard-api` | 8087 | REST API, SQLite persistence, score validation |
| `scoreboard.web` | `beggar-scoreboard-web` | 8088 | Razor Pages leaderboard UI |
| `beggarverify` | `beggar-verify` | — | Replays decks to verify submitted scores |
| `beggareventbus` | `rabbitmq:3-management` | 5672 / 15672 | Message bus between API and verifier |
| `beggarcompute-*` | `beggar-compute` | — | Search workers (one container per algorithm) |

All services are defined in [`docker-compose.yml`](../docker-compose.yml). Compute containers share a single Docker image (`beggar-compute`) and are differentiated by the `Algorithm` environment variable.

## Data Flow

```
┌─────────────────────────────────────┐
│         Compute workers             │
│  (brute-force, SA, ILS, memetic…)  │
└──────────────┬──────────────────────┘
               │ POST /api/scores  (JSON game result)
               ▼
┌──────────────────────────────────────────┐
│            Scoreboard API                │
│  • stores score in SQLite (unverified)   │
│  • publishes to RabbitMQ verify queue    │
│  • returns qualifying threshold to caller│
└───────┬──────────────────┬───────────────┘
        │ SQLite reads      │ publish to RabbitMQ
        ▼                   ▼
┌───────────────┐   ┌──────────────────────────┐
│ Scoreboard Web│   │    Beggar Verify          │
│ (leaderboard) │   │  • consumes verify queue  │
│               │   │  • replays deck           │
└───────────────┘   │  • publishes result       │
                    └──────────┬───────────────┘
                               │ consume result queue
                               ▼
                    ┌──────────────────────────┐
                    │     Scoreboard API        │
                    │  • sets IsVerified = true │
                    └──────────────────────────┘
```

## Score Submission Pipeline

1. A compute worker finds a game longer than its current `bestSubmitted` and longer than the per-strategy threshold.
2. It `POST`s a `ScoreRequest` JSON body to `scoreboard.api`.
3. The API validates the payload, stores the score as `IsVerified = false`, and publishes a `VerifyRequest` message to RabbitMQ.
4. The API returns the new threshold (the 1000th-highest score for this `(players, strategy)` bucket) to the caller.
5. `beggarverify` picks up the message, constructs a fresh `Game` from the stored deck, replays it, and checks the move count matches. On success it publishes a `VerifyResponse`.
6. The API's RabbitMQ consumer sets `IsVerified = true` on the score record.

The `SkipVerification` API flag (see [configuration](configuration.md)) bypasses step 3–6, accepting all scores immediately — useful for local development or trusted workers.

## Database

SQLite file at the path given by `ConnectionStrings__ScoreBoardDatabase` (default `/data/scoreboard.db`, persisted in the `beggar-data` Docker volume).

EF Core migrations run automatically on startup. The `Scores` table:

| Column | Type | Notes |
|---|---|---|
| `Id` | INTEGER PK | Auto-increment |
| `User` | TEXT | Submitter name |
| `Length` | INTEGER | Move count (the score) |
| `Deck` | TEXT | JSON array of 52 integers |
| `Players` | INTEGER | Number of players |
| `Strategy` | TEXT | Algorithm strategy name |
| `Version` | TEXT | Compute client version |
| `InstanceId` | TEXT | Per-process GUID |
| `Team` | TEXT | Optional team label |
| `Iteration` | INTEGER | Iteration counter at submission time |
| `Submitted` | DATETIME | UTC timestamp |
| `IsVerified` | INTEGER | 0 / 1 |

## Threshold System

The API maintains an in-memory threshold per `(players, strategy)` pair. The threshold is the lowest score in the current top-1000 for that bucket. Compute workers fetch their threshold on startup via `GET /api/scores/threshold?players=N&strategy=X` and receive an updated threshold in the `POST` response after each submission. This prevents workers from flooding the scoreboard with scores below the qualifying bar.

See [API reference](api.md) for full endpoint details.
