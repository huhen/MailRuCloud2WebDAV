using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
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
            uint ui = 0xffffffff;
            ushort us = 0x400f;

            var b = Encoding.Default.GetBytes(dir);
            var len = b.Length + 1;
            if (len > 255) len = 255;

            var bui = BitConverter.GetBytes(ui);
            var bus = BitConverter.GetBytes(us);

            using (var ms = new MemoryStream())
            {
                ms.WriteByte(0x75);
                ms.WriteByte((byte)len);
                ms.Write(b, 0, len - 1);
                ms.WriteByte(0x0);
                ms.Write(bui, 0, bui.Length);
                ms.Write(bus, 0, bus.Length);
                ms.WriteByte(0x0);
                return ms.ToArray();
            }
        }

        private DirResponse parseDirResponse(byte[] data)
        {
            var r = new DirResponse();
            if (data == null || data.Length == 0)
            {
                r.Status = 1;
                return r;
            }

            r.Status = data[0];

            if (r.Status != 0 || data.Length == 1) return r;
            r.Status1 = data[1];

            if (data.Length < 14) return r;

            var versionString = Encoding.Default.GetString(data, 2, 12);
            r.Version = long.Parse(versionString, NumberStyles.AllowHexSpecifier);
            return r;
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
                using (var content = new ByteArrayContent(test))
                {
                    content.Headers.ContentType = new MediaTypeHeaderValue(Common.ApplicationXwwwFormUrlencoded);

                    using (var response = Client.PostAsync(GetUriString(token), content, Cts.Token).Result)
                    {
                        if (!response.IsSuccessStatusCode)
                        {
                            return false;
                        }
                        var tmp = response.Content.ReadAsByteArrayAsync().Result;
                        var ttaat = BitConverter.ToString(tmp);
                        var ttttt = parseDirResponse(tmp);
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
