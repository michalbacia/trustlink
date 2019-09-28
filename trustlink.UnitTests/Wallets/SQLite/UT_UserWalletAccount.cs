using Microsoft.VisualStudio.TestTools.UnitTesting;
using Trustlink.Wallets;

namespace Trustlink.UnitTests.Wallets.SQLite
{
    [TestClass]
    public class UT_UserWalletAccount
    {
        [TestMethod]
        public void TestGenerator()
        {
            UserWalletAccount account = new UserWalletAccount(UInt160.Zero);
            Assert.IsNotNull(account);
        }

        [TestMethod]
        public void TestGetHasKey()
        {
            UserWalletAccount account = new UserWalletAccount(UInt160.Zero);
            Assert.AreEqual<bool>(false, account.HasKey);
        }

        [TestMethod]
        public void TestGetKey()
        {
            UserWalletAccount account = new UserWalletAccount(UInt160.Zero);
            Assert.AreEqual<KeyPair>(null, account.GetKey());
        }
    }
}
