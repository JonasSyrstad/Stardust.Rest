using System;
using System.Threading;
using System.Threading.Tasks;

namespace Stardust.Interstellar.Rest.Extensions
{
        public static class PeriodicTask
        {
            public static void Run(Action<object, CancellationToken> doWork, object taskState, TimeSpan period, CancellationToken cancellationToken)
            {
                Task.Run(async () =>
                {
                    do
                    {
                        await Task.Delay(period, cancellationToken).ConfigureAwait(false);
                        cancellationToken.ThrowIfCancellationRequested();
                        doWork(taskState, cancellationToken);
                    } while (true);
                });
            }
        }
}