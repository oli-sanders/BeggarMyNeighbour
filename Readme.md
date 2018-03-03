I have always wanted to know if the card game "Beggar my Neighbour" can ever get into an infinite loop.
I have also been wanting to experiment with ASP.NET core and containerised microservices, so I wrote a distributed system to find the longest games of "Beggar my Neighbour".

### Quick Links
- [Live Web]

### Technologies Used
- [ASP.Net Core](https://www.asp.net/)
- [Docker](https://www.docker.com/)
- [RabbitMQ](http://www.rabbitmq.com/)

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
Plays virtual games of "Beggar my Neighbour" and submits games to the scoreboard API service.
Can be run remotely and submits scores without authentication.

### Beggar.Scoreboard.API
Back end for the scoreboard. Stores the current high scores.
Receives new scores from beggar.compute instances.
Retrieves scores for the web front end.

### Beggar.Verify
Runs alongside the scoreboard API Service to verify received games from anonymous clients

Communicates with the scoreboard using [RabbitMQ](http://www.rabbitmq.com/)

### Beggar.Scoreboard.Web
Web front end for the scoreboard presents and filters scores submitted to the scoreboard API
