using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Trustlink.Cryptography;
using Trustlink.IO.Json;
using Trustlink.Wallets;
using Trustlink.Wallets.TLP6;


namespace Trustlink.UnitTests.Wallets.TLP6
{
    [TestClass]
    public class UT_TLP6Account
    {
        TLP6Account account;
        UInt160 hash;
        TLP6Wallet wallet;
        private static string tlp2;
        private static KeyPair keyPair;

        [ClassInitialize]
        public static void ClassSetup(TestContext context)
        {
            byte[] privateKey = { 0x01,0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01,
                0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01};
            keyPair = new KeyPair(privateKey);
            tlp2 = keyPair.Export("Satoshi", 0, 0, 0);
        }

        [TestInitialize]
        public void TestSetup()
        {
            wallet = TestUtils.GenerateTestWallet();
            byte[] array1 = { 0x01 };
            hash = new UInt160(Crypto.Default.Hash160(array1));
            account = new TLP6Account(wallet, hash);
        }

        [TestMethod]
        public void TestConstructorWithNep2Key()
        {
            account.ScriptHash.Should().Be(hash);
            account.Decrypted.Should().BeTrue();
            account.HasKey.Should().BeFalse();
        }

        [TestMethod]
        public void TestConstructorWithKeyPair()
        {
            TLP6Wallet wallet = new TLP6Wallet("a");
            byte[] array1 = { 0x01 };
            var hash = new UInt160(Crypto.Default.Hash160(array1));
            string password = "hello world";
            var account = new TLP6Account(wallet, hash, keyPair, password);
            account.ScriptHash.Should().Be(hash);
            account.Decrypted.Should().BeTrue();
            account.HasKey.Should().BeTrue();
        }

        [TestMethod]
        public void TestFromJson()
        {
            JObject json = new JObject();
            json["address"] = "ARxgjcH2K1yeW5f5ryuRQNaBzSa9TZzmVS";
            json["key"] = null;
            json["label"] = null;
            json["isDefault"] = true;
            json["lock"] = false;
            json["contract"] = null;
            json["extra"] = null;
            var account = TLP6Account.FromJson(json, wallet);
            account.ScriptHash.Should().Be("ARxgjcH2K1yeW5f5ryuRQNaBzSa9TZzmVS".ToScriptHash());
            account.Label.Should().BeNull();
            account.IsDefault.Should().BeTrue();
            account.Lock.Should().BeFalse();
            account.Contract.Should().BeNull();
            account.Extra.Should().BeNull();
            account.GetKey().Should().BeNull();

            json["key"] = "6PYRjVE1gAbCRyv81FTiFz62cxuPGw91vMjN4yPa68bnoqJtioreTznezn";
            json["label"] = "label";
            account = TLP6Account.FromJson(json, wallet);
            account.Label.Should().Be("label");
            account.HasKey.Should().BeTrue();
        }

        [TestMethod]
        public void TestGetKey()
        {
            account.GetKey().Should().BeNull();
            wallet.Unlock("Satoshi");
            account = new TLP6Account(wallet, hash, tlp2);
            account.GetKey().Should().Be(keyPair);
        }

        [TestMethod]
        public void TestGetKeyWithString()
        {
            account.GetKey("Satoshi").Should().BeNull();
            account = new TLP6Account(wallet, hash, tlp2);
            account.GetKey("Satoshi").Should().Be(keyPair);
        }

        [TestMethod]
        public void TestToJson()
        {
            JObject nep6contract = new JObject();
            nep6contract["script"] = "2103603f3880eb7aea0ad4500893925e4a42fea48a44ee6f898a10b3c7ce05d2a267ac";
            JObject parameters = new JObject();
            parameters["type"] = 0x00;
            parameters["name"] = "Sig";
            JArray array = new JArray
            {
                parameters
            };
            nep6contract["parameters"] = array;
            nep6contract["deployed"] = false;
            account.Contract = TLP6Contract.FromJson(nep6contract);
            JObject json = account.ToJson();
            json["address"].Should().Equals("AZk5bAanTtD6AvpeesmYgL8CLRYUt5JQsX");
            json["label"].Should().BeNull();
            json["isDefault"].ToString().Should().Be("false");
            json["lock"].ToString().Should().Be("false");
            json["key"].Should().BeNull();
            json["contract"]["script"].ToString().Should().Be("\"2103603f3880eb7aea0ad4500893925e4a42fea48a44ee6f898a10b3c7ce05d2a267ac\"");
            json["extra"].Should().BeNull();

            account.Contract = null;
            json = account.ToJson();
            json["contract"].Should().BeNull();
        }

        [TestMethod]
        public void TestVerifyPassword()
        {
            account = new TLP6Account(wallet, hash, tlp2);
            account.VerifyPassword("Satoshi").Should().BeTrue();
            account.VerifyPassword("b").Should().BeFalse();
        }
    }
}
