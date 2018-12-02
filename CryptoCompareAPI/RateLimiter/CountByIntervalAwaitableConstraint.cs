using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CryptoCompareAPI.RateLimiter
{
    public class CountByIntervalAwaitableConstraint : IAwaitableConstraint
    {
        public IReadOnlyList<DateTime> TimeStamps => _TimeStamps.ToList();

        protected LimitedSizeStack<DateTime> _TimeStamps { get; }

        int _Count { get; }
        TimeSpan _TimeSpan { get; }
        SemaphoreSlim _Semafore { get; } = new SemaphoreSlim(1, 1);
        ITime _Time { get; }

        public CountByIntervalAwaitableConstraint(int count, TimeSpan timeSpan, ITime time = null)
        {
            if (count <= 0)
                throw new ArgumentException("count should be strictly positive", nameof(count));

            if (timeSpan.TotalMilliseconds <= 0)
                throw new ArgumentException("timeSpan should be strictly positive", nameof(timeSpan));

            _Count = count;
            _TimeSpan = timeSpan;
            _TimeStamps = new LimitedSizeStack<DateTime>(_Count);
            _Time = time ?? TimeSystem.StandardTime;
        }

        public async Task<IDisposable> WaitForReadiness(CancellationToken cancellationToken)
        {
            await _Semafore.WaitAsync(cancellationToken);
            var count = 0;
            var now = _Time.GetNow();
            var target = now - _TimeSpan;
            LinkedListNode<DateTime> element = _TimeStamps.First, last = null;
            while ((element != null) && (element.Value > target))
            {
                last = element;
                element = element.Next;
                count++;
            }

            if (count < _Count)
                return new DisposeAction(OnEnded);

            var timetoWait = last.Value.Add(_TimeSpan) - now;
            Console.WriteLine("TimeWait: {0}; count: {1}; _Count: {2}", timetoWait.Seconds, count, _Count);
            try
            {
                await _Time.GetDelay(timetoWait, cancellationToken);
            }
            catch (Exception)
            {
                _Semafore.Release();
                throw;
            }

            return new DisposeAction(OnEnded);
        }

        void OnEnded()
        {
            var now = _Time.GetNow();
            _TimeStamps.Push(now);
            OnEnded(now);
            _Semafore.Release();
        }

        protected virtual void OnEnded(DateTime now)
        {
        }
    }
}