/* Copyright (c) 2017 Oliver Sanders

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
*/
using Microsoft.Extensions.Logging;
using System.Collections.Generic;

namespace CardGames.BeggarMyNeighbour
{
    public class Game
    {
        private List<int> _deck;
        private List<Player> _players;
        private int CurrentPlayer;

        public int PreviousPlayer;
        private readonly ILogger _logger;
        private Queue<int> _stack;
        private int _cardsPlayed;

        public Game(List<int> deck, int players) : this(null, deck, players)
        {
        }

        public Game(ILogger logger, List<int> deck, int players)
        {
            _logger = logger;
            _players = new List<Player>();
            _deck = deck;
            for (int i = 0; i < players; i++)
            {
                _players.Add(new Player(i));
            }

            Deal();

            _cardsPlayed = 0;

            _stack = new Queue<int>();
        }

        private void Deal()
        {
            CurrentPlayer = 0;
            while (_deck.Count != 0)
            {
                _logger?.LogInformation($"Dealt {_deck[0]} to Player {CurrentPlayer}");
                _players[CurrentPlayer].Addcard(_deck[0]);
                _deck.RemoveAt(0);
                NextPlayer();
            }
        }

        void NextPlayer()
        {
            CurrentPlayer++;
            if (CurrentPlayer >= _players.Count)
            {
                CurrentPlayer = 0;
            }
            _logger?.LogDebug($"Moving to player {_players[CurrentPlayer].ID} Pos : {CurrentPlayer}");
        }

        public int Play()
        {
            var paycount = 0;
            CurrentPlayer = 0;
            PreviousPlayer = -1;

            while (_players.Count > 1)
            {
                int currentcard = PlayCard();                

                if (paycount > 0)
                {
                    if (currentcard > 0)
                    {
                        if (_players.Find(p => p.ID == PreviousPlayer).Count == 0)
                        {
                            //previous player is out
                            var pos = _players.FindIndex(p => p.ID == PreviousPlayer);
                            RemovePlayer(pos);

                            CurrentPlayer--;
                            if (CurrentPlayer == -1)
                            {
                                CurrentPlayer = 0;
                            }
                        }  
                    }
                    else
                    { 
                        //only reduce paycount if card is not a picture card
                        paycount--;
                    }

                    if (paycount == 0)
                    {
                        //player has lost the penalty give stack to previous
                        _players.Find(p => p.ID == PreviousPlayer).AddStack(_stack);
                        _logger?.LogInformation($"{_players[CurrentPlayer].ID} pos : {CurrentPlayer} has lost the penalty the stack goes to {PreviousPlayer} pos : {_players.FindIndex(p => p.ID == PreviousPlayer)}");
                        PreviousPlayer = -1;
                    }
                }

                if (currentcard > 0)
                {
                    //if card is a picture card
                    paycount = currentcard;
                    PreviousPlayer = _players[CurrentPlayer].ID;
                    _logger?.LogInformation($"Paycount is now {paycount}");
                    //move to next player
                    NextPlayer();
                }
                else
                {
                    //if player has not played a picture card they may be out.
                    if (_players[CurrentPlayer].Count == 0)
                    {
                        RemovePlayer(CurrentPlayer);
                        CurrentPlayer--;
                        if (CurrentPlayer == -1)
                        {
                            CurrentPlayer = 0;
                        }

                        if (paycount>0)
                        {
                            _logger?.LogInformation("Test");
                            NextPlayer();
                        }
                    }
                }

                if(paycount ==0)
                {
                    NextPlayer();
                }
            }
            return _cardsPlayed;
        }

        private void RemovePlayer(int Player)
        {
            //player is out
            _logger?.LogInformation($"player {_players[Player].ID} Pos : {Player} is out of the game");
            _players.RemoveAt(Player);
        }

        private int PlayCard()
        {
            var currentcard = _players[CurrentPlayer].Playcard();
            _stack.Enqueue(currentcard);
            _cardsPlayed++;
            _logger?.LogInformation($"player {_players[CurrentPlayer].ID} Pos : {CurrentPlayer} Plays {currentcard}");
            return currentcard;
        }
    }
}
