using System;
using System.Threading;

namespace UKHO.ConfigurableStub
{
    public static class TimingHelper
    {
        private static readonly TimeSpan OneHundredMilliseconds = TimeSpan.FromMilliseconds(100);

        public static bool WaitFor(Func<bool> predicate, double timeoutSeconds = 5.0, bool throwExceptionIfPredicateNotTrue = true)
        {
            var endTime = DateTime.UtcNow.AddSeconds(timeoutSeconds);
            Exception lastException = null;
            bool predicateResult = false;

            do
            {
                try
                {
                    predicateResult = predicate();
                }
                catch (Exception excepton)
                {
                    lastException = excepton;
                }
                Thread.Sleep(OneHundredMilliseconds);
            }
            while (!predicateResult && DateTime.UtcNow < endTime);

            // Throw exception if predicate not true (and required), so as better identify problem; rather than muddying waters with null ref exceptions.
            if (throwExceptionIfPredicateNotTrue && !predicateResult)
            {
                if (lastException != null)
                    throw new Exception($"TimingHelper.WaitFor Predicate failed: The last exception thrown was : {lastException}");

                throw new Exception("TimingHelper.WaitFor Predicate failed");
            }

            return predicateResult;
        }
    }
}