using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Trustlink.UnitTests.Wallets.SQLite
{
    [TestClass]
    public class UT_Address
    {
        [TestMethod]
        public void TestGenerator()
        {
            Address address = new Address();
            Assert.IsNotNull(address);
        }

        [TestMethod]
        public void TestSetAndGetScriptHash()
        {
            Address address = new Address
            {
                ScriptHash = new byte[] { 0x01 }
            };
            Assert.AreEqual(Encoding.Default.GetString(new byte[] { 0x01 }), Encoding.Default.GetString(address.ScriptHash));
        }
    }
}
