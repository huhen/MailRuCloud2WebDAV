using System;
using System.Diagnostics;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;

namespace MailRuCloud2WebDAV.CloudApi
{
    internal class BaseApiObject : IDisposable
    {
        private int _inWork;
        private object _inWorkLock;

        private HttpMessageHandler _httpMessageHandler;

        protected DispatcherApi Dispatcher;
        protected HttpClient Client;
        protected CancellationTokenSource Cts;
        protected bool Disposed;

        protected BaseApiObject(HttpMessageHandler httpMessageHandler, CancellationTokenSource cts, DispatcherApi dispatcher)
        {
            Cts = cts;
            Dispatcher = dispatcher;
            _httpMessageHandler = httpMessageHandler;
            _inWorkLock = new object();
            //Client = null;
            CreateClient();
        }

        private void CreateClient()
        {
            Client = new HttpClient(_httpMessageHandler, false);
            Client.DefaultRequestHeaders.Add(Common.UserAgent, Common.UserAgentString);
            Client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("*/*"));
            //AfterCreateClient();
        }

        //protected virtual void AfterCreateClient()
        //{
        //}

        protected void SafeIncInWork()
        {
            Monitor.Enter(_inWorkLock);
            _inWork++;
            Monitor.Exit(_inWorkLock);
        }

        protected void SafeDecInWork()
        {
            Monitor.Enter(_inWorkLock);
            _inWork--;
            Monitor.Exit(_inWorkLock);
        }

        protected bool RefreshUrlIfNeed(string url)
        {
            //if (Client == null) CreateClient();

            if (url == null) return false;

            if (Client.BaseAddress == null)
            {
                Client.BaseAddress = new Uri(url);
            }
            else if (!Client.BaseAddress.OriginalString.Equals(url))
            {
                Monitor.Enter(_inWorkLock);
                try
                {
                    if (_inWork != 0) return true;
                    Client.Dispose();
                    CreateClient();
                    Client.BaseAddress = new Uri(url);
                }
                catch (Exception exception)
                {
                    Debug.WriteLine(exception);
                    return false;
                }
                finally
                {
                    Monitor.Exit(_inWorkLock);
                }
            }
            return true;
        }

        public void Dispose()
        {
            if (Disposed) return;
            Disposed = true;

            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                Cts.Cancel();
                Client?.Dispose();
            }
        }
    }
}
