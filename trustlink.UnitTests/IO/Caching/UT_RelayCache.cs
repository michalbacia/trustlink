using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Trustlink.Network.P2P.Payloads;

namespace Trustlink.UnitTests.IO.Caching
{
    [TestClass]
    public class UT_RelayCache
    {
        RelayCache relayCache;

        [TestInitialize]
        public void SetUp()
        {
            relayCache = new RelayCache(10);
        }

        [TestMethod]
        public void TestGetKeyForItem()
        {
            Transaction tx = new Transaction()
            {
                Version = 0,
                Nonce = 1,
                Sender = UInt160.Parse("0xa400ff00ff00ff00ff00ff00ff00ff00ff00ff01"),
                SystemFee = 0,
                NetworkFee = 0,
                ValidUntilBlock = 100,
                Cosigners = new Cosigner[0],
                Attributes = new TransactionAttribute[0],
                Script = new byte[] { 0x00, 0x01, 0x02, 0x03, 0x04 },
                Witnesses = new Witness[0]
            };
            relayCache.Add(tx);

            AssertionExtensions.Should((bool) relayCache.Contains(tx)).BeTrue();
            AssertionExtensions.Should((bool) relayCache.TryGet(tx.Hash, out IInventory tmp)).BeTrue();
            (tmp is Transaction).Should().BeTrue();
        }
    }
}
