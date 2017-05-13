using System.Collections.Generic;
using System.Linq;

namespace beggar
{
    class Game
    {
        private List<int> _deck;
        private List<Player> _players;
        private int CurrentPlayer;

        public int PreviousPlayer;

        // private int PreviousPlayer;



        public Game(List<int> deck, int players)
        {
            _players = new List<Player>();
            _deck = deck;
            for (int i = 0; i < players; i++)
            {
                _players.Add(new Player(i));
            }

            Deal();
        }

        private void Deal()
        {
            CurrentPlayer = 0;
            while (_deck.Count != 0)
            {
                _players[CurrentPlayer].Addcard(_deck[0]);
                _deck.RemoveAt(0);
                CurrentPlayer++;
                if (CurrentPlayer == _players.Count)
                {
                    CurrentPlayer = 0;
                }
            }
        }

        void NextPlayer()
        {
            PreviousPlayer = CurrentPlayer;
            CurrentPlayer++;
            if (CurrentPlayer >= _players.Count)
            {
                CurrentPlayer = 0;
            }
        }
        

        public int Play()
        {
            var CardsPlayed = 0;

            var stack = new Queue<int>();
            var paycount = 0;

            CurrentPlayer = 0;
            while (_players.Count > 1)
            {
                if (paycount > 0)
                {
                    while (paycount > 0)
                    {

                        var currentcard = _players[CurrentPlayer].Playcard();
                        stack.Enqueue(currentcard);
                        CardsPlayed++;

                        if (currentcard < 0)
                        {
                            //if card is a picture card
                            paycount = (0 - currentcard) + 1;

                            if (_players[PreviousPlayer].Count == 0)
                            {
                                //previous player is out
                                _players.RemoveAt(PreviousPlayer);
                                CurrentPlayer--;
                                PreviousPlayer--;
                                if (CurrentPlayer == -1)
                                {
                                    CurrentPlayer = 0;
                                    PreviousPlayer = _players.Count - 1;
                                }
                            }

                            //move to next player
                            NextPlayer();
                        }
                        else
                        {
                            //if player has not played a picture card they may be out.
                            if (_players[CurrentPlayer].Count == 0)
                            {
                                //player is out
                                _players.RemoveAt(CurrentPlayer);
                                PreviousPlayer--;
                                if(PreviousPlayer < 0)
                                {
                                    PreviousPlayer = _players.Count - 1;
                                }
                                if (CurrentPlayer >= _players.Count)
                                {
                                    CurrentPlayer = 0;
                                }
                                if (_players.Count == 1)
                                {
                                    //player has won prepare to finish game
                                    paycount = 1;
                                    PreviousPlayer = CurrentPlayer;
                                }
                            }


                        }

                        paycount--;
                    }

                    //player has lost the penalty give stack to previous
                    _players[PreviousPlayer].AddStack(stack);

                }
                else
                {
                    var currentcard = _players[CurrentPlayer].Playcard();

                    //add to stack
                    stack.Enqueue(currentcard);

                    CardsPlayed++;

                    if (currentcard < 0)
                    {
                        //if card is a picture card
                        paycount = 0 - currentcard;

                    }
                    else
                    {
                        //if player has not played a picture card they may be out.
                        if (_players[CurrentPlayer].Count == 0)
                        {
                            //player is out
                            _players.RemoveAt(CurrentPlayer);
                        }
                    }
                }



                //move to next player
                NextPlayer();

            }

            return CardsPlayed;
        }

        
    }
}