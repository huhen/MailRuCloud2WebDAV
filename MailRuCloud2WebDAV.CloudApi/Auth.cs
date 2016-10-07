using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Security.Authentication;
using System.Threading;

namespace MailRuCloud2WebDAV.CloudApi
{
    internal class Auth : IDisposable
    {
        [DataContract]
        private class ErrorAuthApiResponse
        {
            [DataMember(Name = "error")]
            internal string Error;
            [DataMember(Name = "error_code")]
            internal int ErrorCode;
            [DataMember(Name = "error_description")]
            internal string ErrorDescription;
        }

        [DataContract]
        private class AuthApiResponse
        {
            [DataMember(Name = "expires_in")]
            internal int ExpiresIn;
            [DataMember(Name = "refresh_token")]
            internal string RefreshToken;
            [DataMember(Name = "access_token")]
            internal string AccessToken;
        }

        internal string Token
        {
            get
            {
                if (_disposed) throw new ObjectDisposedException("Auth");

                var currTick = Environment.TickCount;
                if ((currTick > 0 && _expiresIn > 0) || (currTick < 0 && _expiresIn < 0))
                {
                    if (_expiresIn - currTick < 0) TryRefreshToken();
                }
                else if (currTick < 0 && _expiresIn > 0)
                {
                    TryRefreshToken();
                }

                return _token;
            }
        }

        private string _token;
        private string _refreshToken;
        private long _expiresIn;

        private Dispatcher _dispatcher;

        private HttpClient _client;

        private CancellationTokenSource _cts;
        private bool _disposed;

        internal Auth(HttpMessageHandler httpMessageHandler, CancellationTokenSource cts, Dispatcher dispatcher)
        {
            _cts = cts;
            _dispatcher = dispatcher;
            _token = string.Empty;
            ResetRefreshToken();

            _client = new HttpClient(httpMessageHandler, false);
            _client.DefaultRequestHeaders.Add(Common.UserAgent, Common.UserAgentString);
        }

        internal bool TryRefreshToken()
        {
            if (_disposed) throw new ObjectDisposedException("Auth");

            try
            {
                _token = string.Empty;
                if (string.IsNullOrEmpty(_refreshToken))
                {
                    ResetRefreshToken();
                    return false;
                }

                _client.BaseAddress = new Uri(_dispatcher.AuthUrl);

                using (var content = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("client_id", "cloud-win"),
                    new KeyValuePair<string, string>("grant_type", "refresh_token"),
                    new KeyValuePair<string, string>("refresh_token", _refreshToken)
                }))
                {
                    using (var response = _client.PostAsync(string.Empty, content, _cts.Token).Result)
                    {
                        if (!response.IsSuccessStatusCode)
                        {
                            ResetRefreshToken();
                            return false;
                        }

                        return ReadStream(response.Content.ReadAsStreamAsync().Result);
                    }
                }
            }
            catch (Exception exception)
            {
                ResetRefreshToken();

                Debug.WriteLine(exception);
            }

            return false;
        }

        internal bool Login(string username, string password)
        {
            if (_disposed) throw new ObjectDisposedException("Auth");

            try
            {
                _token = string.Empty;
                _client.BaseAddress = new Uri(_dispatcher.AuthUrl);

                using (var content = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("client_id", "cloud-win"),
                    new KeyValuePair<string, string>("grant_type", "password"),
                    new KeyValuePair<string, string>("username", username),
                    new KeyValuePair<string, string>("password", password)
                }))
                {
                    using (var response = _client.PostAsync(string.Empty, content, _cts.Token).Result)
                    {
                        if (!response.IsSuccessStatusCode)
                        {
                            ResetRefreshToken();
                            return false;
                        }

                        return ReadStream(response.Content.ReadAsStreamAsync().Result);
                    }
                }
            }
            catch (Exception exception)
            {
                Debug.WriteLine(exception);
            }
            return false;
        }

        private bool ReadStream(Stream stream)
        {
            try
            {
                var ser = new DataContractJsonSerializer(typeof(AuthApiResponse));
                var authApiResponse = (AuthApiResponse)ser.ReadObject(stream);
                if (authApiResponse.AccessToken == null) throw new SerializationException();

                _token = authApiResponse.AccessToken;
                _refreshToken = authApiResponse.RefreshToken;
                _expiresIn = Common.GetNextTickCount(authApiResponse.ExpiresIn * 1000);
                return true;
            }
            catch (SerializationException)
            {
                var ser = new DataContractJsonSerializer(typeof(ErrorAuthApiResponse));
                stream.Position = 0;
                var errorAuthApiResponse = (ErrorAuthApiResponse)ser.ReadObject(stream);
                throw new AuthenticationException($"ErrorCode: {errorAuthApiResponse.ErrorCode} Error: {errorAuthApiResponse.Error} ErrorDescription: {errorAuthApiResponse.ErrorDescription}");
            }
        }

        private void ResetRefreshToken()
        {
            _refreshToken = string.Empty;
            _expiresIn = Environment.TickCount;
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
