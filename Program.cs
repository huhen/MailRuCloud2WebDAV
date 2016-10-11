using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using MailRuCloud2WebDAV.CloudApi;

namespace MailRuCloud2WebDAV
{
    class Program
    {
        static void Main(string[] args)
        {
            //var t = new MailRuCloudApi(new WebProxy("localhost", 8888));
            var t = new MailRuCloudApi();
            var s = t.Auth("autoscop@mail.ru", "baza-0000");
            var k = t.Test();
        }
    }
}
