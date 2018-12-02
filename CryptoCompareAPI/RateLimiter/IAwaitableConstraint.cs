using System;
using System.Threading;
using System.Threading.Tasks;

namespace CryptoCompareAPI.RateLimiter
{
    internal interface IAwaitableConstraint
    {
        Task<IDisposable> WaitForReadiness(CancellationToken cancellationToken);
    }
}