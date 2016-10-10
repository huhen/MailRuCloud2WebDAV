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
    internal class Auth : BaseApiObject
    {
        internal string Token
        {
            get
            {
                if (Disposed) throw new ObjectDisposedException("Auth");

                if (Common.CheckTick(_expiresIn)) TryRefreshToken();

                return _token;
            }
        }

        private string _token;
        private string _refreshToken;
        private int _expiresIn;

        internal Auth(HttpMessageHandler httpMessageHandler, CancellationTokenSource cts, DispatcherApi dispatcher) : base(httpMessageHandler, cts, dispatcher)
        {
            _token = string.Empty;
            ResetRefreshToken();
        }

        internal bool TryRefreshToken()
        {
            if (Disposed) throw new ObjectDisposedException("Auth");

            SafeIncInWork();

            try
            {
                _token = string.Empty;
                if (string.IsNullOrEmpty(_refreshToken))
                {
                    ResetRefreshToken();
                    return false;
                }

                if (!RefreshUrlIfNeed(Dispatcher.AuthUrl)) return false;

                using (var content = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>(Common.ClientId, Common.ClientIdString),
                    new KeyValuePair<string, string>("grant_type", "refresh_token"),
                    new KeyValuePair<string, string>("refresh_token", _refreshToken)
                }))
                {
                    using (var response = Client.PostAsync(string.Empty, content, Cts.Token).Result)
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
            catch (AuthenticationException)
            {
                throw;
            }
            catch (Exception exception)
            {
                ResetRefreshToken();
                Debug.WriteLine(exception);
            }
            finally
            {
                SafeDecInWork();
            }

            return false;
        }

        internal bool Login(string username, string password)
        {
            if (Disposed) throw new ObjectDisposedException("Auth");

            SafeIncInWork();

            try
            {
                _token = string.Empty;
                if (!RefreshUrlIfNeed(Dispatcher.AuthUrl)) return false;

                using (var content = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>(Common.ClientId, Common.ClientIdString),
                    new KeyValuePair<string, string>("grant_type", "password"),
                    new KeyValuePair<string, string>("username", username),
                    new KeyValuePair<string, string>("password", password)
                }))
                {
                    using (var response = Client.PostAsync(string.Empty, content, Cts.Token).Result)
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
            catch (AuthenticationException)
            {
                throw;
            }
            catch (Exception exception)
            {
                Debug.WriteLine(exception);
            }
            finally
            {
                SafeDecInWork();
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
    }
}
