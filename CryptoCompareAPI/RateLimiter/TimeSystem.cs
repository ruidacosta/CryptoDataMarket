using System;
using System.Threading;
using System.Threading.Tasks;

namespace CryptoCompareAPI.RateLimiter
{
    public class TimeSystem : ITime
    {
        public static ITime StandardTime { get; }

        static TimeSystem()
        {
            StandardTime = new TimeSystem();
        }

        TimeSystem()
        {
        }

        DateTime ITime.GetNow()
        {
            return DateTime.Now;
        }

        Task ITime.GetDelay(TimeSpan timespan, CancellationToken cancellationToken)
        {
            return Task.Delay(timespan, cancellationToken);
        }
    }
}