namespace CryptoCompareAPI.RateLimiter
{
    static class IAwaitableConstraintExtension
    {
        public static IAwaitableConstraint Compose(this IAwaitableConstraint awaitableConstraint1, IAwaitableConstraint awaitableConstraint2) => awaitableConstraint1 == awaitableConstraint2
                ? awaitableConstraint1
                : new ComposedAwaitableConstraint(awaitableConstraint1, awaitableConstraint2);
    }
}