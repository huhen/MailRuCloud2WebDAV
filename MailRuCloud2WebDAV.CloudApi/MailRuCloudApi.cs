using System;
using System.Net;
using System.Net.Http;
using System.Threading;

namespace MailRuCloud2WebDAV.CloudApi
{
    public class MailRuCloudApi : IDisposable
    {
        private DispatcherApi _dispatcherClient;
        private Auth _authClient;
        private MetaApi _metaClient;
        private HttpClientHandler _httpClientHandler;

        private bool _disposed;
        private CancellationTokenSource _cts;

        public MailRuCloudApi(IWebProxy proxy = null)
        {
            _cts = new CancellationTokenSource();

            _httpClientHandler = new HttpClientHandler
            {
                Proxy = proxy,
                UseProxy = proxy != null,
                PreAuthenticate = true,
                UseDefaultCredentials = false,
                AllowAutoRedirect = false,
                MaxAutomaticRedirections = 1,
                MaxRequestContentBufferSize = int.MaxValue
            };

            _dispatcherClient = new DispatcherApi(_httpClientHandler, _cts);
            _authClient = new Auth(_httpClientHandler, _cts, _dispatcherClient);
            _metaClient = new MetaApi(_httpClientHandler, _cts, _dispatcherClient, _authClient);
        }

        public bool Auth(string username, string password)
        {
            if (_disposed) throw new ObjectDisposedException("MailRuCloudApi");
            return _authClient.Login(username, password);
        }

        public bool Test()
        {
            if (_disposed) throw new ObjectDisposedException("MailRuCloudApi");
            _metaClient.GetDirInfo();
            return false;
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

                _authClient.Dispose();

                _dispatcherClient?.Dispose();
                _httpClientHandler?.Dispose();
            }
        }
    }
}
