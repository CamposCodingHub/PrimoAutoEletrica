using System;
using System.Threading;
using System.Threading.Tasks;

namespace PrimoAutoEletrica.Utilities
{
    /// <summary>
    /// Helper class to execute an asynchronous operation with retry logic.
    /// </summary>
    public static class RetryHelper
    {
        /// <summary>
        /// Executes the provided <paramref name="operation"/> with retry attempts.
        /// </summary>
        /// <typeparam name="TResult">The type of the result returned by the operation.</typeparam>
        /// <param name="operation">A function returning a Task of <typeparamref name="TResult"/>.</param>
        /// <param name="maxAttempts">Maximum number of attempts (default int.MaxValue for unlimited retries).</param>
        /// <param name="initialDelayMs">Initial delay in milliseconds before first retry (default 500ms). Delay doubles after each retry.</param>
        /// <param name="cancellationToken">Optional cancellation token.</param>
        /// <returns>The result of the operation if successful.</returns>
        /// <exception cref="AggregateException">Throws after exhausting attempts.</exception>
        public static async Task<TResult> ExecuteWithRetryAsync<TResult>(
            Func<Task<TResult>> operation,
            int maxAttempts = 5,
            int initialDelayMs = 500,
            CancellationToken cancellationToken = default)
        {
            if (operation == null) throw new ArgumentNullException(nameof(operation));
            int attempt = 0;
            int delay = initialDelayMs;
            var exceptions = new System.Collections.Generic.List<Exception>();
            while (true)
            {
                try
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    return await operation().ConfigureAwait(false);
                }
                catch (Exception ex) when (!(ex is OperationCanceledException))
                {
                    attempt++;
                    exceptions.Add(ex);
                    if (attempt >= maxAttempts)
                        throw new AggregateException($"Operation failed after {maxAttempts} attempts.", exceptions);
                    await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
                    delay *= 2; // exponential backoff
                }
            }
        }

        /// <summary>
        /// Executes a non-returning asynchronous operation with retry logic.
        /// </summary>
        public static async Task ExecuteWithRetryAsync(
            Func<Task> operation,
            int maxAttempts = 5,
            int initialDelayMs = 500,
            CancellationToken cancellationToken = default)
        {
            await ExecuteWithRetryAsync<object>(
                async () => { await operation().ConfigureAwait(false); return null; },
                maxAttempts,
                initialDelayMs,
                cancellationToken).ConfigureAwait(false);
        }
    }
}
