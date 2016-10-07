using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MailRuCloud2WebDAV.CloudApi
{
    public class MailRuCloudApi : IDisposable
    {
        private Dispatcher _dispatcherClient;
        private Auth _authClient;
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

            _dispatcherClient = new Dispatcher(_httpClientHandler, _cts);
            _authClient = new Auth(_httpClientHandler, _cts, _dispatcherClient);

        }


        public string DispatcherM()
        {
            while (!_authClient.Login("safds@mail.ru", "afsdfsadf"))
            {
                Thread.Sleep(1000);
            }
            var s = _authClient.Token;
            _authClient.Login("safds@mail.ru", "afsdfsadf");
            s = _authClient.Token;
            s = _authClient.Token;
            s = _authClient.Token;
            s = _authClient.Token;
            return s;
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

                _dispatcherClient?.Dispose();

                _httpClientHandler?.Dispose();
            }
        }
    }
}
