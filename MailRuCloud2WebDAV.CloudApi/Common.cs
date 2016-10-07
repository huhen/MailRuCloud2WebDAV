using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MailRuCloud2WebDAV.CloudApi
{
    internal static class Common
    {
        internal const string UserAgent = "User-Agent";
        internal const string UserAgentString = "CloudDesktopWindows 15.06.0409 beta 57f68b47";

        internal static async Task RunPeriodicTask(Action<object, CancellationToken> doWork, object taskState, int period, CancellationToken cancellationToken)
        {
            var customDelay = taskState as CustomDelay;

            await Task.Run(() => doWork(taskState, cancellationToken), cancellationToken).ConfigureAwait(false);
            do
            {
                if (customDelay != null) period = customDelay.Delay;
                await Task.Delay(period, cancellationToken).ConfigureAwait(false);
                cancellationToken.ThrowIfCancellationRequested();
                doWork(taskState, cancellationToken);
            }
            while (!cancellationToken.IsCancellationRequested);
        }

        internal static int GetNextTickCount(int add)
        {
            var t = (long)Environment.TickCount + add;
            if (t <= int.MaxValue)
            {
                return (int)t;
            }
            t -= int.MaxValue;
            if (t < 0) t = 0;
            return int.MinValue + (int)t;
        }
    }

    internal class CustomDelay
    {
        internal int Delay;
    }
}
