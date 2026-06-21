# Configuration

All services are configured via environment variables. In Docker Compose these are set under each service's `environment:` block.

---

## Compute Worker (`beggar-compute`)

| Variable | Default | Description |
|---|---|---|
| `Algorithm` | `BruteForce` | Which search algorithm to run. See [algorithms](algorithms.md) for valid values. |
| `BeggarUser` | _(none)_ | Name submitted with every score. |
| `BeggarTeam` | _(none)_ | Optional team label attached to every submission. |
| `BeggarPlayers` | `4` | Number of players for simulated games. Affects which scoreboard bucket scores go into. |
| `ScoreboardUrl` | `http://beggar-api.o-os.uk` | Full URL of the scoreboard's `POST /api/scores` endpoint (no trailing slash). |
| `BeggarVersion` | _(assembly version)_ | Version string reported with each score. |
| `InstanceId` | _(random GUID per process)_ | Stable identifier for this compute process, reported with every submission. Set explicitly if you want consistent tracking across restarts. |

### Algorithm values

| Value | Strategy name |
|---|---|
| `BruteForce` | `brute-force` |
| `HillClimb` | `hill-climb` |
| `HillClimbParallel` | `hill-climb-parallel` |
| `SimulatedAnnealing` | `simulated-annealing` |
| `SimulatedAnnealingAdaptive` | `simulated-annealing-adaptive` |
| `IteratedLocalSearch` | `iterated-local-search` |
| `Memetic` | `memetic` |
| `TabuSearch` | `tabu-search` |
| `Genetic` | `genetic` |

---

## Scoreboard API (`scoreboard.api`)

Configured via `appsettings.json` or environment variable overrides (standard ASP.NET Core convention — replace `:` with `__`).

| Setting | Environment variable | Default | Description |
|---|---|---|---|
| `ConnectionStrings:ScoreBoardDatabase` | `ConnectionStrings__ScoreBoardDatabase` | `Data Source=scoreboard.db;` | SQLite connection string. In Docker Compose the file is stored in the `beggar-data` volume at `/data/scoreboard.db`. |
| `RabbitMq:HostName` | `RabbitMq__HostName` | `beggareventbus` | Hostname of the RabbitMQ server used for the verification pipeline. |
| `SkipVerification` | `SkipVerification` | _(not set)_ | Set to `true` to accept all submitted scores without replaying them through `beggarverify`. Useful for local development or trusted environments. |
| `UpstreamScoreboard:Url` | `UpstreamScoreboard__Url` | _(not set)_ | If set, scores that enter the top 1000 are forwarded to this parent scoreboard URL. Enables hierarchical scoreboards. |
| `RateLimit:PermitLimit` | — | `30` | Maximum submissions per window per client IP. |
| `RateLimit:WindowSeconds` | — | `10` | Rate limit window in seconds. |

---

## Scoreboard Web (`scoreboard.web`)

| Setting | Environment variable | Default | Description |
|---|---|---|---|
| `ScoreboardApi:BaseUrl` | `ScoreboardApi__BaseUrl` | `http://scoreboard.api` | Base URL (no trailing slash) of the scoreboard API. In Docker Compose this is `http://scoreboard.api:8080`. |

---

## Verify Service (`beggarverify`)

| Variable | Default | Description |
|---|---|---|
| `RabbitMqHostName` | `beggareventbus` | Hostname of the RabbitMQ server to consume verification requests from. |

---

## Docker Compose Ports

| Service | Host port | Container port | Protocol |
|---|---|---|---|
| `scoreboard.api` | 8087 | 8080 | HTTP |
| `scoreboard.web` | 8088 | 8080 | HTTP |
| `beggareventbus` (AMQP) | 5672 | 5672 | AMQP |
| `beggareventbus` (management) | 15672 | 15672 | HTTP |
