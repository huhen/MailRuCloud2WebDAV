using System;
using System.Diagnostics;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;

namespace MailRuCloud2WebDAV.CloudApi
{
    internal class DispatcherApi : IDisposable
    {
        //w-weblink
        //e-video
        //r-reginfo
        //t-thumb
        //y-dwl
        //u-upload
        //a-auth(swa)
        //s-s.mail.ru(screenshoter?)
        //d-oauth-get
        //z-docdl
        //x-dmeta
        //v-view
        //n-notify
        internal string AuthUrl
        {
            get
            {
                if (_disposed) throw new ObjectDisposedException("Dispatcher");
                if (Common.CheckTick(_expiresAuthUrlIn)) RefreshAuthUrl();
                return _authUrl;
            }
        }
        private int _expiresAuthUrlIn;
        private string _authUrl;

        internal string MetaUrl
        {
            get
            {
                if (_disposed) throw new ObjectDisposedException("Dispatcher");
                if (Common.CheckTick(_expiresMetaUrlIn)) RefreshMetaUrl();
                return _metaUrl;
            }
        }
        private int _expiresMetaUrlIn;
        private string _metaUrl;

        private const string _dispatcherUrl = "https://dispatcher.cloud.mail.ru";
        private const int _dispatcherRefreshPeriod = 15 * 60 * 1000;

        private HttpClient _client;

        private CancellationTokenSource _cts;
        private bool _disposed;

        internal DispatcherApi(HttpMessageHandler httpMessageHandler, CancellationTokenSource cts)
        {
            _cts = cts;

            _client = new HttpClient(httpMessageHandler, false) { BaseAddress = new Uri(_dispatcherUrl) };
            _client.DefaultRequestHeaders.Add(Common.UserAgent, Common.UserAgentString);
            _client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("*/*"));

            RefreshAuthUrl();
            RefreshMetaUrl();
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

        private void RefreshMetaUrl()
        {
            try
            {
                using (var responseMsg = _client.GetAsync("m", _cts.Token).Result)
                {
                    _metaUrl = ParseResponse(responseMsg.Content.ReadAsStringAsync().Result);
                    _expiresMetaUrlIn = Common.GetNextTickCount(_dispatcherRefreshPeriod);
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
