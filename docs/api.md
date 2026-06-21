# API Reference

Base URL (Docker Compose): `http://localhost:8087`

All request and response bodies are `application/json`.

---

## POST `/api/scores`

Submit a new game result. Returns the current qualifying threshold for the `(players, strategy)` bucket.

### Request body (`ScoreRequest`)

| Field | Type | Required | Description |
|---|---|---|---|
| `User` | string | yes | Name to attribute the score to |
| `Length` | integer | yes | Number of moves before the game ended |
| `Deck` | integer[] | yes | 52-element deck array (0 = number card, 1–4 = picture card value) |
| `Players` | integer | yes | Number of players (typically 2 or 4) |
| `Strategy` | string | yes | Algorithm strategy name (e.g. `simulated-annealing`) |
| `Version` | string | no | Compute client version string |
| `InstanceId` | string | no | Per-process identifier (GUID) |
| `Team` | string | no | Optional team label |
| `Iteration` | integer | no | Algorithm iteration counter at submission time |

### Response

Returns a plain integer — the new qualifying threshold score for the `(players, strategy)` bucket. Compute workers use this to update their local threshold without making an additional GET request.

If `SkipVerification` is not set, the score is stored as unverified and queued for replay by `beggarverify`. The `IsVerified` flag is set asynchronously.

Submissions are rate-limited (30 per 10 seconds per client IP).

---

## GET `/api/scores`

Returns the top 1000 games for a player count, ordered by score descending.

### Query parameters

| Parameter | Type | Default | Description |
|---|---|---|---|
| `players` | integer | _(all)_ | Filter by number of players |

### Response body (`ScoreResponse[]`)

| Field | Type | Description |
|---|---|---|
| `user` | string | Submitter name |
| `length` | integer | Move count (score) |
| `submitted` | datetime | UTC submission timestamp |
| `deck` | integer[] | 52-element deck |
| `isVerified` | boolean | Whether the deck has been replayed and confirmed |
| `players` | integer | Number of players |
| `strategy` | string | Algorithm strategy name |
| `version` | string | Compute client version |
| `instanceId` | string | Per-process GUID |
| `team` | string | Team label (may be null) |
| `iteration` | integer | Iteration counter at submission (may be null) |

---

## GET `/api/scores/threshold`

Returns the current qualifying threshold for a `(players, strategy)` bucket — the lowest score currently in the top 1000 for that bucket. Compute workers call this on startup.

### Query parameters

| Parameter | Type | Required | Description |
|---|---|---|---|
| `players` | integer | yes | Number of players |
| `strategy` | string | yes | Strategy name (URL-encoded) |

### Response

Plain integer. Returns `0` if the bucket has fewer than 1000 entries (any score qualifies).

---

## GET `/api/scores/history`

Returns a downsampled history of all submitted scores for charting — one best score per `(strategy, minute)` bucket, ordered by submission time. Used by the web front end to draw the improvement-over-time scatter chart.

### Query parameters

| Parameter | Type | Default | Description |
|---|---|---|---|
| `players` | integer | _(all)_ | Filter by number of players |
| `limit` | integer | `5000` | Maximum number of data points returned |

### Response body

Array of objects:

| Field | Type | Description |
|---|---|---|
| `strategy` | string | Algorithm strategy name |
| `submitted` | datetime | UTC timestamp of the submission |
| `length` | integer | Score (move count) |

---

## GET `/health`

ASP.NET health check endpoint. Returns `200 OK` with body `Healthy` when the service is running. Used by Docker Compose health checks.
