using System;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace MailRuCloud2WebDAV.CloudApi
{
    internal static class Common
    {
        internal const string UserAgent = "User-Agent";
        internal const string UserAgentString = "CloudDesktopWindows 15.06.0409 beta 57f68b47";

        internal const string ClientId = "client_id";
        internal const string ClientIdString = "cloud-win";

        internal const string ApplicationXwwwFormUrlencoded = "application/x-www-form-urlencoded";

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

        internal static bool CheckTick(int expiresIn)
        {
            var currTick = Environment.TickCount;
            if ((currTick > 0 && expiresIn > 0) || (currTick < 0 && expiresIn < 0))
            {
                if (expiresIn - currTick < 0) return true;
            }
            else if (currTick < 0 && expiresIn > 0)
            {
                return true;
            }
            return false;
        }
    }

    [DataContract]
    public class ErrorAuthApiResponse
    {
        [DataMember(Name = "error")]
        public string Error;
        [DataMember(Name = "error_code")]
        public int ErrorCode;
        [DataMember(Name = "error_description")]
        public string ErrorDescription;
    }

    [DataContract]
    public class AuthApiResponse
    {
        [DataMember(Name = "expires_in")]
        public int ExpiresIn;
        [DataMember(Name = "refresh_token")]
        public string RefreshToken;
        [DataMember(Name = "access_token")]
        public string AccessToken;
    }

    public class DirResponse
    {
        public byte Status;
        public byte Status1;
        public long Version;
    }

    public class CustomDelay
    {
        public int Delay;
    }
}
