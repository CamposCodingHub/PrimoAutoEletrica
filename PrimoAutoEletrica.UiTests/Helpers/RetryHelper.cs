using System;
using System.Diagnostics;
using System.Threading;

namespace PrimoAutoEletrica.UiTests.Helpers
{
    /// <summary>
    /// Utility class to perform retry / wait operations in UI tests.
    /// </summary>
    public static class RetryHelper
    {
        /// <summary>
        /// Executes the supplied <paramref name="condition"/> repeatedly until it returns true or the timeout expires.
        /// </summary>
        /// <param name="condition">A function that returns true when the desired state is reached.</param>
        /// <param name="timeoutSeconds">Maximum time to wait in seconds (default 5).</param>
        /// <param name="pollIntervalMs">Interval between checks in milliseconds (default 200).</param>
        public static void RetryWhile(Func<bool> condition, int timeoutSeconds = 5, int pollIntervalMs = 200)
        {
            var sw = Stopwatch.StartNew();
            while (!condition())
            {
                if (sw.Elapsed.TotalSeconds > timeoutSeconds)
                    throw new TimeoutException($"Condição não satisfeita após {timeoutSeconds} segundos.");
                Thread.Sleep(pollIntervalMs);
            }
        }
    }
}
