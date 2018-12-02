using System;
using System.Collections.Generic;

namespace CryptoCompareAPI.RateLimiter
{
    public class PersistentCountByIntervalAwaitableConstraint : CountByIntervalAwaitableConstraint
    {
        readonly Action<DateTime> _saveStateAction;

        /// <summary>
        /// Create an instance of <see cref="PersistentCountByIntervalAwaitableConstraint"/>.
        /// </summary>
        /// <param name="count">Maximum actions allowed per time interval.</param>
        /// <param name="timeSpan">Time interval limits are applied for.</param>
        /// <param name="saveStateAction">Action is used to save state.</param>
        /// <param name="initialTimeStamps">Initial timestamps.</param>
        public PersistentCountByIntervalAwaitableConstraint(int count, TimeSpan timeSpan,
            Action<DateTime> saveStateAction, IEnumerable<DateTime> initialTimeStamps, ITime time = null) : base(count, timeSpan, time)
        {
            _saveStateAction = saveStateAction;

            if (initialTimeStamps == null)
                return;

            foreach (var timeStamp in initialTimeStamps)
            {
                _TimeStamps.Push(timeStamp);
            }
        }

        /// <summary>
        /// Save state
        /// </summary>
        protected override void OnEnded(DateTime now)
        {
            _saveStateAction(now);
        }
    }
}