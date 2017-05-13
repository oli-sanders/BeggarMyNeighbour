using System;
using System.Collections.Generic;
namespace beggar
{
    internal class Player
    {
        private Queue<int> _hand;

        internal int ID { get; private set; }

        internal Player(int id)
        {
            ID = id;
            _hand = new Queue<int>();
        }

        internal int Count
        {
            get
            {
                return _hand.Count;
            }
        }

        internal int Playcard()
        {
            if (_hand.Count == 0)
            {
                return 0;
            }

            return _hand.Dequeue();
        }

        internal void Addcard(int v)
        {
            _hand.Enqueue(v);
        }

        internal void AddStack(Queue<int> stack)
        {
            while(stack.Count > 0)
            {
                _hand.Enqueue(stack.Dequeue());
            }
        }

    }
}