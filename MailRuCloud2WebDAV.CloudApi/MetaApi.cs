using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Security.Authentication;
using System.Text;
using System.Threading;
using System.Web;

namespace MailRuCloud2WebDAV.CloudApi
{
    internal class MetaApi : BaseApiObject
    {
        private Auth _auth;

        internal MetaApi(HttpMessageHandler httpMessageHandler, CancellationTokenSource cts, DispatcherApi dispatcher, Auth auth) : base(httpMessageHandler, cts, dispatcher)
        {
            _auth = auth;
        }

        private string GetUriString(string token)
        {
            return
                $"?{Common.ClientId}={HttpUtility.UrlEncode(Common.ClientIdString)}&token={HttpUtility.UrlEncode(token)}";
        }

        private byte[] BuildRequestDir(string dir)
        {
            var b = Encoding.Default.GetBytes(dir);
            using (var ms = new MemoryStream())
            {
                ms.WriteByte(0x75);

                return ms.ToArray();
            }
        }

        internal bool GetDirInfo()
        {
            if (Disposed) throw new ObjectDisposedException("MetaApi");

            SafeIncInWork();

            try
            {
                var token = _auth.Token;
                if (string.IsNullOrEmpty(token))
                {
                    return false;
                }

                if (!RefreshUrlIfNeed(Dispatcher.MetaUrl)) return false;

                byte[] arrOutput = { 0x75, 0x02, 0x2F, 0x00, 0xFF, 0xFF, 0xFF, 0xFF, 0x0F, 0x41, 0x00 };
                var test = BuildRequestDir("/");
                //byte[] arrOutput = { 0x75, 0x04, 0x74, 0x74, 0x74, 0x00, 0xFF, 0xFF, 0xFF, 0xFF, 0x0F, 0x41, 0x00 };
                using (var content = new ByteArrayContent(arrOutput))
                {
                    content.Headers.ContentType = new MediaTypeHeaderValue(Common.ApplicationXwwwFormUrlencoded);

                    using (var response = Client.PostAsync(GetUriString(token), content, Cts.Token).Result)
                    {
                        if (!response.IsSuccessStatusCode)
                        {
                            return false;
                        }
                        var tmp = response.Content.ReadAsByteArrayAsync().Result;
                        return true;
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

        //protected override void AfterCreateClient()
        //{
        //    Client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("*/*"));
        //}

        /*private bool ReadStream(Stream stream)
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
        }*/
    }
}
