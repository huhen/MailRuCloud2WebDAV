using Microsoft.VisualStudio.TestTools.UnitTesting;
using MailRuCloud2WebDAV.CloudApi;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MailRuCloud2WebDAV.CloudApi.Tests
{
    [TestClass()]
    public class MailRuCloudApiTests
    {
        [TestMethod()]
        public void MailRuCloudApiTest()
        {
            try
            {
                var t = new MailRuCloudApi();
                var s = t.DispatcherM();
                
                Assert.Fail(s);
            }
            catch (Exception e)
            {
                Assert.Fail(e.ToString());
            }
        }

    }
}