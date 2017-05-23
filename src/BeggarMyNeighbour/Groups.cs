using System;
using System.Collections.Generic;
using System.Text;

namespace BeggarMyNeighbour
{
   public class Groups
    {
        Dictionary<int, int> _groups = new Dictionary<int, int>();
        private int _multiplier;

        public Groups(int multiplier)
        {
            _multiplier = multiplier;
        }

        public void Add(int value)
        {
            var currentgroup = (int)Math.Floor(value / (decimal)_multiplier) * _multiplier;

            if (_groups.ContainsKey(currentgroup))
            {
                _groups[currentgroup]++;
            }
            else
            {
                _groups.Add(currentgroup, 1);
            }
        }

        public int Value(int group) => _groups[group];
    }
}
