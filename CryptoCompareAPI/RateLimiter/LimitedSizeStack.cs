using System.Collections.Generic;

namespace CryptoCompareAPI.RateLimiter
{
    public class LimitedSizeStack<T> : LinkedList<T>
    {
        readonly int _MaxSize;
        public LimitedSizeStack(int maxSize)
        {
            _MaxSize = maxSize;
        }

        public void Push(T item)
        {
            AddFirst(item);

            if (Count > _MaxSize)
                RemoveLast();
        }
    }
}