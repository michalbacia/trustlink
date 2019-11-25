using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Trustlink.IO.Json;
using Trustlink.SmartContract;
using Trustlink.Wallets;
using Trustlink.Wallets.SQLite;
using Trustlink.Wallets.TLP6;

namespace Trustlink.UnitTests.Wallets.TLP6
{
    [TestClass]
    public class UT_TLP6Wallet
    {
        private TLP6Wallet uut;
        private static string wPath;
        private static KeyPair keyPair;
        private static string nep2key;
        private static UInt160 testScriptHash;

        public static string GetRandomPath()
        {
            string threadName = Thread.CurrentThread.ManagedThreadId.ToString();
            return Path.GetFullPath(string.Format("Wallet_{0}", new Random().Next(1, 1000000).ToString("X8")) + threadName);
        }

        [ClassInitialize]
        public static void ClassInit(TestContext context)
        {
            byte[] privateKey = new byte[32];
            using (RandomNumberGenerator rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(privateKey);
            }
            keyPair = new KeyPair(privateKey);
            testScriptHash = Contract.CreateSignatureContract(keyPair.PublicKey).ScriptHash;
            nep2key = keyPair.Export("123", 0, 0, 0);
        }

        private TLP6Wallet CreateWallet()
        {
            return TestUtils.GenerateTestWallet();
        }

        private string CreateWalletFile()
        {
            string path = GetRandomPath();
            if (!Directory.Exists(path)) Directory.CreateDirectory(path);
            path = Path.Combine(path, "wallet.json");
            File.WriteAllText(path, "{\"name\":\"name\",\"version\":\"0.0\",\"scrypt\":{\"n\":0,\"r\":0,\"p\":0},\"accounts\":[],\"extra\":{}}");
            return path;
        }

        [TestInitialize]
        public void TestSetup()
        {
            uut = CreateWallet();
            wPath = CreateWalletFile();
        }

        [TestCleanup]
        public void TestCleanUp()
        {
            if (File.Exists(wPath)) File.Delete(wPath);
        }

        [TestMethod]
        public void TestConstructorWithPathAndName()
        {
            NEP6Wallet wallet = new NEP6Wallet(wPath);
            Assert.AreEqual("name", wallet.Name);
            Assert.AreEqual(new ScryptParameters(0, 0, 0).ToJson().ToString(), wallet.Scrypt.ToJson().ToString());
            Assert.AreEqual(new Version().ToString(), wallet.Version.ToString());
            wallet = new NEP6Wallet("", "test");
            Assert.AreEqual("test", wallet.Name);
            Assert.AreEqual(ScryptParameters.Default.ToJson().ToString(), wallet.Scrypt.ToJson().ToString());
            Assert.AreEqual(Version.Parse("1.0"), wallet.Version);
        }

        [TestMethod]
        public void TestConstructorWithJObject()
        {
            JObject wallet = new JObject();
            wallet["name"] = "test";
            wallet["version"] = Version.Parse("1.0").ToString();
            wallet["scrypt"] = ScryptParameters.Default.ToJson();
            wallet["accounts"] = new JArray();
            wallet["extra"] = new JObject();
            wallet.ToString().Should().Be("{\"name\":\"test\",\"version\":\"1.0\",\"scrypt\":{\"n\":16384,\"r\":8,\"p\":8},\"accounts\":[],\"extra\":{}}");
            NEP6Wallet w = new NEP6Wallet(wallet);
            Assert.AreEqual("test", w.Name);
            Assert.AreEqual(Version.Parse("1.0").ToString(), w.Version.ToString());
        }

        [TestMethod]
        public void TestGetName()
        {
            Assert.AreEqual("noname", uut.Name);
        }

        [TestMethod]
        public void TestGetVersion()
        {
            Assert.AreEqual(new System.Version().ToString(), uut.Version.ToString());
        }

        [TestMethod]
        public void TestContains()
        {
            bool result = uut.Contains(testScriptHash);
            Assert.AreEqual(false, result);
            uut.CreateAccount(testScriptHash);
            result = uut.Contains(testScriptHash);
            Assert.AreEqual(true, result);
        }

        [TestMethod]
        public void TestAddCount()
        {
            uut.CreateAccount(testScriptHash);
            Assert.IsTrue(uut.Contains(testScriptHash));
            WalletAccount account = uut.GetAccount(testScriptHash);
            Assert.IsTrue(account.WatchOnly);
            Assert.IsFalse(account.HasKey);
            uut.Unlock("123");
            uut.CreateAccount(keyPair.PrivateKey);
            account = uut.GetAccount(testScriptHash);
            Assert.IsFalse(account.WatchOnly);
            Assert.IsTrue(account.HasKey);
            uut.CreateAccount(testScriptHash);
            account = uut.GetAccount(testScriptHash);
            Assert.IsFalse(account.WatchOnly);
            Assert.IsFalse(account.HasKey);
            uut.CreateAccount(keyPair.PrivateKey);
            account = uut.GetAccount(testScriptHash);
            Assert.IsFalse(account.WatchOnly);
            Assert.IsTrue(account.HasKey);
        }

        [TestMethod]
        public void TestCreateAccountWithPrivateKey()
        {
            bool result = uut.Contains(testScriptHash);
            Assert.AreEqual(false, result);
            uut.Unlock("123");
            uut.CreateAccount(keyPair.PrivateKey);
            result = uut.Contains(testScriptHash);
            Assert.AreEqual(true, result);
        }

        [TestMethod]
        public void TestCreateAccountWithKeyPair()
        {
            Contract contract = Contract.CreateSignatureContract(keyPair.PublicKey);
            bool result = uut.Contains(testScriptHash);
            Assert.AreEqual(false, result);
            uut.CreateAccount(contract);
            result = uut.Contains(testScriptHash);
            Assert.AreEqual(true, result);
            uut.DeleteAccount(testScriptHash);
            result = uut.Contains(testScriptHash);
            Assert.AreEqual(false, result);
            uut.Unlock("123");
            uut.CreateAccount(contract, keyPair);
            result = uut.Contains(testScriptHash);
            Assert.AreEqual(true, result);
        }

        [TestMethod]
        public void TestCreateAccountWithScriptHash()
        {
            bool result = uut.Contains(testScriptHash);
            Assert.AreEqual(false, result);
            uut.CreateAccount(testScriptHash);
            result = uut.Contains(testScriptHash);
            Assert.AreEqual(true, result);
        }

        [TestMethod]
        public void TestDecryptKey()
        {
            string nep2key = keyPair.Export("123", 0, 0, 0);
            uut.Unlock("123");
            KeyPair key1 = uut.DecryptKey(nep2key);
            bool result = key1.Equals(keyPair);
            Assert.AreEqual(true, result);
        }

        [TestMethod]
        public void TestDeleteAccount()
        {
            bool result = uut.Contains(testScriptHash);
            Assert.AreEqual(false, result);
            uut.CreateAccount(testScriptHash);
            result = uut.Contains(testScriptHash);
            Assert.AreEqual(true, result);
            uut.DeleteAccount(testScriptHash);
            result = uut.Contains(testScriptHash);
            Assert.AreEqual(false, result);
        }

        [TestMethod]
        public void TestGetAccount()
        {
            bool result = uut.Contains(testScriptHash);
            Assert.AreEqual(false, result);
            uut.Unlock("123");
            uut.CreateAccount(keyPair.PrivateKey);
            result = uut.Contains(testScriptHash);
            Assert.AreEqual(true, result);
            WalletAccount account = uut.GetAccount(testScriptHash);
            Assert.AreEqual(Contract.CreateSignatureContract(keyPair.PublicKey).Address, account.Address);
        }

        [TestMethod]
        public void TestGetAccounts()
        {
            Dictionary<string, KeyPair> keys = new Dictionary<string, KeyPair>();
            uut.Unlock("123");
            byte[] privateKey = new byte[32];
            using (RandomNumberGenerator rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(privateKey);
            }
            KeyPair key = new KeyPair(privateKey);
            Contract contract = Contract.CreateSignatureContract(key.PublicKey);
            keys.Add(contract.Address, key);
            keys.Add(Contract.CreateSignatureContract(keyPair.PublicKey).Address, keyPair);
            uut.CreateAccount(key.PrivateKey);
            uut.CreateAccount(keyPair.PrivateKey);
            foreach (var account in uut.GetAccounts())
            {
                if (!keys.TryGetValue(account.Address, out KeyPair k))
                {
                    Assert.Fail();
                }
            }
        }

        public X509Certificate2 NewCertificate()
        {
            ECDsa key = ECDsa.Create(ECCurve.NamedCurves.nistP256);
            CertificateRequest request = new CertificateRequest(
                "CN=Self-Signed ECDSA",
                key,
                HashAlgorithmName.SHA256);
            request.CertificateExtensions.Add(
                new X509KeyUsageExtension(X509KeyUsageFlags.DigitalSignature, critical: false));
            request.CertificateExtensions.Add(
                new X509BasicConstraintsExtension(false, false, 0, false));
            DateTimeOffset start = DateTimeOffset.UtcNow;
            X509Certificate2 cert = request.CreateSelfSigned(notBefore: start, notAfter: start.AddMonths(3));
            return cert;
        }

        [TestMethod]
        public void TestImportCert()
        {
            X509Certificate2 cert = NewCertificate();
            Assert.IsNotNull(cert);
            Assert.AreEqual(true, cert.HasPrivateKey);
            uut.Unlock("123");
            WalletAccount account = uut.Import(cert);
            Assert.IsNotNull(account);
        }

        [TestMethod]
        public void TestImportWif()
        {
            string wif = keyPair.Export();
            bool result = uut.Contains(testScriptHash);
            Assert.AreEqual(false, result);
            uut.Unlock("123");
            uut.Import(wif);
            result = uut.Contains(testScriptHash);
            Assert.AreEqual(true, result);
        }

        [TestMethod]
        public void TestImportNep2()
        {
            bool result = uut.Contains(testScriptHash);
            Assert.AreEqual(false, result);
            uut.Import(nep2key, "123", 0, 0, 0);
            result = uut.Contains(testScriptHash);
            Assert.AreEqual(true, result);
            uut.DeleteAccount(testScriptHash);
            result = uut.Contains(testScriptHash);
            Assert.AreEqual(false, result);
            JObject wallet = new JObject();
            wallet["name"] = "name";
            wallet["version"] = new Version().ToString();
            wallet["scrypt"] = new ScryptParameters(0, 0, 0).ToJson();
            wallet["accounts"] = new JArray();
            wallet["extra"] = new JObject();
            uut = new NEP6Wallet(wallet);
            result = uut.Contains(testScriptHash);
            Assert.AreEqual(false, result);
            uut.Import(nep2key, "123", 0, 0, 0);
            result = uut.Contains(testScriptHash);
            Assert.AreEqual(true, result);
        }

        [TestMethod]
        public void TestLock()
        {
            Assert.ThrowsException<ArgumentNullException>(() => uut.CreateAccount(keyPair.PrivateKey));
            uut.Unlock("123");
            uut.CreateAccount(keyPair.PrivateKey);
            bool result = uut.Contains(testScriptHash);
            Assert.AreEqual(true, result);
            uut.DeleteAccount(testScriptHash);
            uut.Lock();
            Assert.ThrowsException<ArgumentNullException>(() => uut.CreateAccount(keyPair.PrivateKey));
        }

        [TestMethod]
        public void TestMigrate()
        {
            string path = GetRandomPath();
            UserWallet uw = UserWallet.Create(path, "123");
            uw.CreateAccount(keyPair.PrivateKey);
            string npath = Path.Combine(path, "w.json");
            NEP6Wallet nw = NEP6Wallet.Migrate(npath, path, "123");
            bool result = nw.Contains(testScriptHash);
            Assert.AreEqual(true, result);
        }

        [TestMethod]
        public void TestSave()
        {
            JObject wallet = new JObject();
            wallet["name"] = "name";
            wallet["version"] = new System.Version().ToString();
            wallet["scrypt"] = new ScryptParameters(0, 0, 0).ToJson();
            wallet["accounts"] = new JArray();
            wallet["extra"] = new JObject();
            File.WriteAllText(wPath, wallet.ToString());
            uut = new NEP6Wallet(wPath);
            uut.Unlock("123");
            uut.CreateAccount(keyPair.PrivateKey);
            bool result = uut.Contains(testScriptHash);
            Assert.AreEqual(true, result);
            uut.Save();
            result = uut.Contains(testScriptHash);
            Assert.AreEqual(true, result);
        }

        [TestMethod]
        public void TestUnlock()
        {
            Assert.ThrowsException<ArgumentNullException>(() => uut.CreateAccount(keyPair.PrivateKey));
            uut.Unlock("123");
            uut.CreateAccount(keyPair.PrivateKey);
            bool result = uut.Contains(testScriptHash);
            Assert.AreEqual(true, result);
            Assert.ThrowsException<CryptographicException>(() => uut.Unlock("1"));
        }

        [TestMethod]
        public void TestVerifyPassword()
        {
            bool result = uut.VerifyPassword("123");
            Assert.AreEqual(true, result);
            Assert.ThrowsException<ArgumentNullException>(() => uut.CreateAccount(keyPair.PrivateKey));
            uut.Unlock("123");
            uut.CreateAccount(keyPair.PrivateKey);
            result = uut.Contains(testScriptHash);
            Assert.AreEqual(true, result);
            result = uut.VerifyPassword("123");
            Assert.AreEqual(true, result);
            uut.DeleteAccount(testScriptHash);
            Assert.AreEqual(false, uut.Contains(testScriptHash));
            JObject wallet = new JObject();
            wallet["name"] = "name";
            wallet["version"] = new Version().ToString();
            wallet["scrypt"] = new ScryptParameters(0, 0, 0).ToJson();
            wallet["accounts"] = new JArray();
            wallet["extra"] = new JObject();
            uut = new NEP6Wallet(wallet);
            nep2key = keyPair.Export("123", 0, 0, 0);
            uut.Import(nep2key, "123", 0, 0, 0);
            Assert.IsFalse(uut.VerifyPassword("1"));
            Assert.IsTrue(uut.VerifyPassword("123"));
        }

        [TestMethod]
        public void Test_NEP6Wallet_Json()
        {
            uut.Name.Should().Be("noname");
            uut.Version.Should().Be(new Version());
            uut.Scrypt.Should().NotBeNull();
            uut.Scrypt.N.Should().Be(new ScryptParameters(0, 0, 0).N);
        }
    }
}
