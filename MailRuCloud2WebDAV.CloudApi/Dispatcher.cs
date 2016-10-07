using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MailRuCloud2WebDAV.CloudApi
{
    internal class Dispatcher : IDisposable
    {
        public string AuthUrl
        {
            get
            {
                if (_disposed) throw new ObjectDisposedException("Dispatcher");

                var currTick = Environment.TickCount;
                if ((currTick > 0 && _expiresAuthUrlIn > 0) || (currTick < 0 && _expiresAuthUrlIn < 0))
                {
                    if (_expiresAuthUrlIn - currTick < 0) RefreshAuthUrl();
                }
                else if (currTick < 0 && _expiresAuthUrlIn > 0)
                {
                    RefreshAuthUrl();
                }

                return _authUrl;
            }
        }

        private int _expiresAuthUrlIn;
        private string _authUrl;

        private const string _dispatcherUrl = "https://dispatcher.cloud.mail.ru";
        private const int _dispatcherRefreshPeriod = 15 * 60 * 1000;

        private HttpClient _client;

        private CancellationTokenSource _cts;
        private bool _disposed;

        internal Dispatcher(HttpMessageHandler httpMessageHandler, CancellationTokenSource cts)
        {
            _cts = cts;

            _client = new HttpClient(httpMessageHandler, false) { BaseAddress = new Uri(_dispatcherUrl) };
            _client.DefaultRequestHeaders.Add(Common.UserAgent, Common.UserAgentString);

            RefreshAuthUrl();
        }

        private void RefreshAuthUrl()
        {
            try
            {
                using (var responseMsg = _client.GetAsync("o", _cts.Token).Result)
                {
                    _authUrl = ParseResponse(responseMsg.Content.ReadAsStringAsync().Result);
                    _expiresAuthUrlIn = Common.GetNextTickCount(_dispatcherRefreshPeriod);
                }
            }
            catch (Exception exception)
            {
                Debug.WriteLine(exception);
            }
        }

        private string ParseResponse(string response)
        {
            if (string.IsNullOrEmpty(response)) return string.Empty;
            var r = response.Split(' ');
            if (r.Length == 0) return string.Empty;
            return r[0];
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _cts.Cancel();
                _client?.Dispose();
            }
        }
    }
}
