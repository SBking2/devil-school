
using System.Collections.Generic;

namespace EGame
{
    public class FixedSizeQueue<T> : List<T>
    {
        private readonly int _LimitSize = 1;

        public FixedSizeQueue(int limit)
        {
            _LimitSize = limit;
        }

        public void EnQueue(T value)
        {
            if(Count + 1 > _LimitSize)
                RemoveAt(Count - 1);
            Insert(0, value);
        }
    }
}