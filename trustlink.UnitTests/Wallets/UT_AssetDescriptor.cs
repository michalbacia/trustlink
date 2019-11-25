using System;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Trustlink.Persistence;
using Trustlink.SmartContract.Native;
using Trustlink.Wallets;

namespace Trustlink.UnitTests.Wallets
{
    [TestClass]
    public class UT_AssetDescriptor
    {
        private Store Store;

        [TestInitialize]
        public void TestSetup()
        {
            TestBlockchain.InitializeMockNeoSystem();
            Store = TestBlockchain.GetStore();
        }

        [TestMethod]
        public void TestConstructorWithNonexistAssetId()
        {
            Action action = () =>
            {
                var descriptor = new AssetDescriptor(UInt160.Parse("01ff00ff00ff00ff00ff00ff00ff00ff00ff00a4"));
            };
            action.ShouldThrow<ArgumentException>();
        }

        [TestMethod]
        public void Check_LINK()
        {
            var descriptor = new AssetDescriptor(NativeContract.LINK.Hash);
            descriptor.AssetId.Should().Be(NativeContract.LINK.Hash);
            descriptor.AssetName.Should().Be("LINK");
            descriptor.ToString().Should().Be("LINK");
            descriptor.Decimals.Should().Be(8);
        }

        [TestMethod]
        public void Check_TRUST()
        {
            var descriptor = new AssetDescriptor(NativeContract.TRUST.Hash);
            descriptor.AssetId.Should().Be(NativeContract.TRUST.Hash);
            descriptor.AssetName.Should().Be("TRUST");
            descriptor.ToString().Should().Be("TRUST");
            descriptor.Decimals.Should().Be(0);
        }
    }
}
