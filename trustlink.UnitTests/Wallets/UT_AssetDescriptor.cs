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
        public void Check_GAS()
        {
            var descriptor = new AssetDescriptor(NativeContract.GAS.Hash);
            descriptor.AssetId.Should().Be(NativeContract.GAS.Hash);
            descriptor.AssetName.Should().Be("GAS");
            descriptor.ToString().Should().Be("GAS");
            descriptor.Decimals.Should().Be(8);
        }

        [TestMethod]
        public void Check_NEO()
        {
            var descriptor = new AssetDescriptor(NativeContract.NEO.Hash);
            descriptor.AssetId.Should().Be(NativeContract.NEO.Hash);
            descriptor.AssetName.Should().Be("NEO");
            descriptor.ToString().Should().Be("NEO");
            descriptor.Decimals.Should().Be(0);
        }
    }
}
