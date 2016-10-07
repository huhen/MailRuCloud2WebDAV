using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MailRuCloud2WebDAV.CloudApi;

namespace MailRuCloud2WebDAV
{
    class Program
    {
        static void Main(string[] args)
        {
            var t = new MailRuCloudApi();
            var s = t.DispatcherM();
        }
    }
}
