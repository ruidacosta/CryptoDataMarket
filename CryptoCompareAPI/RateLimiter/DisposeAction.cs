using System;

namespace CryptoCompareAPI.RateLimiter
{
    public class DisposeAction : IDisposable
    {
        Action _Act;

        public DisposeAction(Action act)
        {
            _Act = act;
        }

        public void Dispose()
        {
            _Act?.Invoke();
            _Act = null;
        }
    }
}