using System;
using System.Threading;
using System.Threading.Tasks;

namespace CryptoCompareAPI.RateLimiter
{
    class ComposedAwaitableConstraint : IAwaitableConstraint
    {
        readonly IAwaitableConstraint _AwaitableConstraint1;
        readonly IAwaitableConstraint _AwaitableConstraint2;
        readonly SemaphoreSlim _Semafore = new SemaphoreSlim(1, 1);

        internal ComposedAwaitableConstraint(IAwaitableConstraint awaitableConstraint1, IAwaitableConstraint awaitableConstraint2)
        {
            _AwaitableConstraint1 = awaitableConstraint1;
            _AwaitableConstraint2 = awaitableConstraint2;
        }

        public async Task<IDisposable> WaitForReadiness(CancellationToken cancellationToken)
        {
            await _Semafore.WaitAsync(cancellationToken);
            IDisposable[] diposables;
            try
            {
                diposables = await Task.WhenAll(_AwaitableConstraint1.WaitForReadiness(cancellationToken), _AwaitableConstraint2.WaitForReadiness(cancellationToken));
            }
            catch (Exception)
            {
                _Semafore.Release();
                throw;
            }
            return new DisposeAction(() =>
            {
                foreach (var diposable in diposables)
                {
                    diposable.Dispose();
                }
                _Semafore.Release();
            });
        }
    }
}