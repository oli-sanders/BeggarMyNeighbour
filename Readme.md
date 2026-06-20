# Beggar My Neighbour

[![BCH compliance](https://bettercodehub.com/edge/badge/oli-sanders/BeggarMyNeighbour?branch=master)](https://bettercodehub.com/)

I have always wanted to know if the card game "Beggar my Neighbour" can ever get into an infinite loop.
I have also been wanting to experiment with ASP.NET Core and containerised microservices, so I wrote a distributed system to find the longest games of "Beggar my Neighbour".

### Technologies Used
- [.NET 8](https://dotnet.microsoft.com/) (ASP.NET Core, Razor Pages, EF Core)
- [Docker](https://www.docker.com/) / Docker Compose
- [RabbitMQ](http://www.rabbitmq.com/)
- [SQLite](https://www.sqlite.org/)

## Basic algorithm for beggar my neighbour

1. Deal all the cards in the deck face down. No players look at their cards or shuffles their hand.
2. Each player plays the top card from their hand and places it in a pile face up until a picture card is played.
3. When a picture card is played, the next player has to pay:
	1 card for a Jack;
	2 for a Queen;
	3 for a King;
	4 for an Ace.
4. If another picture card is played while the player is paying for the previous picture card then the next player must pay for the new picture card.
5. When no cards are owed (i.e. the picture card has been paid for without another picture card being played) then the player that played the picture card gets the pile and places it at the bottom of their hand with the cards face down. This player then plays a new card starting a new pile.
6. As soon as a player plays their last card they are out and the next player continues from where the previous player was (e.g. if the previous player still had 2 cards left to play then the new player must pay those 2 cards.)
7. The last player in the game wins.

## Services

### Beggar.Compute
Plays virtual games of "Beggar my Neighbour" and submits long games to the scoreboard API service.
Can be run remotely and submits scores without authentication.
Each submission records the **version** of the compute client, the **strategy** used (e.g. `brute-force`),
a per-process **instance id**, the **user** name, and the **score** (the number of moves before the game ended).

### Beggar.Scoreboard.API
Back end for the scoreboard. Stores the current high scores in SQLite (via EF Core) and keeps the **top 1000 games**
(by number of moves) for each player count.
Receives new scores from Beggar.Compute instances and retrieves scores for the web front end.

### Beggar.Verify
Runs alongside the scoreboard API service to verify received games from anonymous clients by replaying the deck.

Communicates with the scoreboard using [RabbitMQ](http://www.rabbitmq.com/).

### Beggar.Scoreboard.Web
Web front end for the scoreboard. Presents the leaderboard and lets you filter the top games by user, strategy and
number of players. Reads the scores from the Beggar.Scoreboard.API.

## Scoreboard data model

Each game on the scoreboard records:

| Field        | Example                                | Description                                  |
| ------------ | -------------------------------------- | -------------------------------------------- |
| `Version`    | `1.4.6`                                | Version of the compute client                |
| `Strategy`   | `brute-force`                          | Strategy used to find the game               |
| `InstanceId` | `11111111-2222-3333-4444-555555555555` | Identifier of the compute instance           |
| `User`       | `oli`                                  | Name the score was submitted under           |
| `Lenght`     | `4000`                                 | The score: number of moves before game ended |

Only the 1000 highest-scoring games per player count are retained.

## Getting started

### Prerequisites
- [.NET 8 SDK](https://dotnet.microsoft.com/download) (to build and run locally)
- [Docker](https://www.docker.com/) and Docker Compose (to run the full distributed system)

### Build

```sh
dotnet build CardGames.BeggarMyNeighbour.sln
```

### Run the whole system with Docker Compose

```sh
docker compose up --build
```

This starts RabbitMQ, the scoreboard API, the verifier, the compute worker and the web front end. Once running:

| Service                | URL                              |
| ---------------------- | -------------------------------- |
| Scoreboard web (leaderboard) | http://localhost:8088      |
| Scoreboard API         | http://localhost:8087/api/scores |
| RabbitMQ management UI  | http://localhost:15672           |

### Run services individually (local development)

```sh
# Scoreboard API (listens on http://localhost:8087)
dotnet run --project src/CardGames.BeggarMyNeighbour.Scoreboard.API

# Web front end (reads the API at http://localhost:8087 in Development)
dotnet run --project src/CardGames.BeggarMyNeighbour.Scoreboard.Web

# Compute worker
dotnet run --project src/CardGames.BeggarMyNeighbour.Compute
```

## Configuration

### Beggar.Compute (environment variables)

| Variable        | Default                       | Description                                            |
| --------------- | ----------------------------- | ------------------------------------------------------ |
| `Algorithm`     | `Best`                        | Which search algorithm to run                          |
| `BeggarUser`    | _(none)_                      | Name to submit scores under                            |
| `ScoreboardUrl` | `http://beggar-api.o-os.uk`   | Full URL of the scoreboard's `POST /api/scores` endpoint |
| `BeggarVersion` | _(assembly version)_          | Version string reported with each score                |
| `InstanceId`    | _(random GUID per process)_   | Identifier reported with each score                    |

### Beggar.Scoreboard.API (configuration / environment variables)

| Setting                                  | Default                       | Description                          |
| ---------------------------------------- | ----------------------------- | ------------------------------------ |
| `ConnectionStrings__ScoreBoardDatabase`  | `Data Source=scoreboard.db;`  | SQLite connection string             |
| `RabbitMq__HostName`                     | `beggareventbus`              | RabbitMQ host used for verification   |

### Beggar.Scoreboard.Web

| Setting                  | Default                     | Description                       |
| ------------------------ | --------------------------- | --------------------------------- |
| `ScoreboardApi__BaseUrl` | `http://scoreboard.api`     | Base URL of the scoreboard API    |

## API

| Method | Route         | Description                                              |
| ------ | ------------- | ------------------------------------------------------- |
| `GET`  | `/api/scores` | Returns the top 1000 games, highest number of moves first |
| `POST` | `/api/scores` | Submits a new game; returns the current qualifying score threshold |

## License

Released under the [MIT License](LICENSE.txt).
