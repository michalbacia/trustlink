using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Trustlink.UnitTests.Wallets.SQLite
{
    [TestClass]
    public class UT_Key
    {
        [TestMethod]
        public void TestGenerator()
        {
            Key key = new Key();
            Assert.IsNotNull(key);
        }

        [TestMethod]
        public void TestSetAndGetName()
        {
            Key key = new Key
            {
                Name = "AAA"
            };
            Assert.AreEqual<string>("AAA", key.Name);
        }

        [TestMethod]
        public void TestSetAndGetValue()
        {
            Key key = new Key
            {
                Value = new byte[] { 0x01 }
            };
            Assert.AreEqual(Encoding.Default.GetString(new byte[] { 0x01 }), Encoding.Default.GetString(key.Value));
        }
    }
}
