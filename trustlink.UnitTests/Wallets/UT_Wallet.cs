using System;
using System.Collections.Generic;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Trustlink.Cryptography.ECC;
using Trustlink.Ledger;
using Trustlink.Network.P2P.Payloads;
using Trustlink.Persistence;
using Trustlink.SmartContract;
using Trustlink.SmartContract.Native;
using Trustlink.SmartContract.Native.Tokens;
using Trustlink.UnitTests.Cryptography;
using Trustlink.Wallets;
using Trustlink.Wallets.SQLite;

namespace Trustlink.UnitTests.Wallets
{
    internal class MyWallet : Wallet
    {
        public override string Name => "MyWallet";

        public override Version Version => Version.Parse("0.0.1");

        Dictionary<UInt160, WalletAccount> accounts = new Dictionary<UInt160, WalletAccount>();

        public override bool Contains(UInt160 scriptHash)
        {
            return accounts.ContainsKey(scriptHash);
        }

        public void AddAccount(WalletAccount account)
        {
            accounts.Add(account.ScriptHash, account);
        }

        public override WalletAccount CreateAccount(byte[] privateKey)
        {
            KeyPair key = new KeyPair(privateKey);
            VerificationContract contract = new VerificationContract
            {
                Script = Contract.CreateSignatureRedeemScript(key.PublicKey),
                ParameterList = new[] { ContractParameterType.Signature }
            };
            MyWalletAccount account = new MyWalletAccount(contract.ScriptHash);
            account.SetKey(key);
            account.Contract = contract;
            AddAccount(account);
            return account;
        }

        public override WalletAccount CreateAccount(Contract contract, KeyPair key = null)
        {
            MyWalletAccount account = new MyWalletAccount(contract.ScriptHash)
            {
                Contract = contract
            };
            account.SetKey(key);
            AddAccount(account);
            return account;
        }

        public override WalletAccount CreateAccount(UInt160 scriptHash)
        {
            MyWalletAccount account = new MyWalletAccount(scriptHash);
            AddAccount(account);
            return account;
        }

        public override bool DeleteAccount(UInt160 scriptHash)
        {
            return accounts.Remove(scriptHash);
        }

        public override WalletAccount GetAccount(UInt160 scriptHash)
        {
            accounts.TryGetValue(scriptHash, out WalletAccount account);
            return account;
        }

        public override IEnumerable<WalletAccount> GetAccounts()
        {
            return accounts.Values;
        }

        public override bool VerifyPassword(string password)
        {
            return true;
        }
    }

    [TestClass]
    public class UT_Wallet
    {
        Store store;
        private static KeyPair glkey;
        private static string nep2Key;

        [ClassInitialize]
        public static void ClassInit(TestContext context)
        {
            glkey = UT_Crypto.generateCertainKey(32);
            nep2Key = glkey.Export("pwd", 0, 0, 0);
        }

        [TestInitialize]
        public void TestSetup()
        {
            store = TestBlockchain.GetStore();
        }

        [TestMethod]
        public void TestContains()
        {
            MyWallet wallet = new MyWallet();
            Action action = () => wallet.Contains(UInt160.Zero);
            action.ShouldNotThrow();
        }

        [TestMethod]
        public void TestCreateAccount1()
        {
            MyWallet wallet = new MyWallet();
            wallet.CreateAccount(new byte[32]).Should().NotBeNull();
        }

        [TestMethod]
        public void TestCreateAccount2()
        {
            MyWallet wallet = new MyWallet();
            Contract contract = Contract.Create(new ContractParameterType[] { ContractParameterType.Boolean }, new byte[] { 1 });
            WalletAccount account = wallet.CreateAccount(contract, UT_Crypto.generateCertainKey(32).PrivateKey);
            account.Should().NotBeNull();

            wallet = new MyWallet();
            account = wallet.CreateAccount(contract, (byte[])(null));
            account.Should().NotBeNull();
        }

        [TestMethod]
        public void TestCreateAccount3()
        {
            MyWallet wallet = new MyWallet();
            Contract contract = Contract.Create(new ContractParameterType[] { ContractParameterType.Boolean }, new byte[] { 1 });
            wallet.CreateAccount(contract, glkey).Should().NotBeNull();
        }

        [TestMethod]
        public void TestCreateAccount4()
        {
            MyWallet wallet = new MyWallet();
            wallet.CreateAccount(UInt160.Zero).Should().NotBeNull();
        }

        [TestMethod]
        public void TestGetName()
        {
            MyWallet wallet = new MyWallet();
            wallet.Name.Should().Be("MyWallet");
        }

        [TestMethod]
        public void TestGetVersion()
        {
            MyWallet wallet = new MyWallet();
            wallet.Version.Should().Be(Version.Parse("0.0.1"));
        }

        [TestMethod]
        public void TestGetAccount1()
        {
            MyWallet wallet = new MyWallet();
            wallet.CreateAccount(UInt160.Parse("522a2b818c308c7a2c77cfdda11763fe043bfb40"));
            WalletAccount account = wallet.GetAccount(ECCurve.Secp256r1.G);
            account.ScriptHash.Should().Be(UInt160.Parse("0x522a2b818c308c7a2c77cfdda11763fe043bfb40"));
        }

        [TestMethod]
        public void TestGetAccount2()
        {
            MyWallet wallet = new MyWallet();
            Action action = () => wallet.GetAccount(UInt160.Zero);
            action.ShouldNotThrow();
        }

        [TestMethod]
        public void TestGetAccounts()
        {
            MyWallet wallet = new MyWallet();
            Action action = () => wallet.GetAccounts();
            action.ShouldNotThrow();
        }

        [TestMethod]
        public void TestGetAvailable()
        {
            MyWallet wallet = new MyWallet();
            Contract contract = Contract.Create(new ContractParameterType[] { ContractParameterType.Boolean }, new byte[] { 1 });
            WalletAccount account = wallet.CreateAccount(contract, glkey.PrivateKey);
            account.Lock = false;

            // Fake balance
            var snapshot = store.GetSnapshot();
            var key = NativeContract.LINK.CreateStorageKey(20, account.ScriptHash);
            var entry = snapshot.Storages.GetAndChange(key, () => new StorageItem
            {
                Value = new Tlp5AccountState().ToByteArray()
            });
            entry.Value = new Tlp5AccountState()
            {
                Balance = 10000 * NativeContract.LINK.Factor
            }
            .ToByteArray();

            wallet.GetAvailable(NativeContract.LINK.Hash).Should().Be(new BigDecimal(1000000000000, 8));

            entry.Value = new Tlp5AccountState()
            {
                Balance = 0
            }
            .ToByteArray();
        }

        [TestMethod]
        public void TestGetBalance()
        {
            MyWallet wallet = new MyWallet();
            Contract contract = Contract.Create(new ContractParameterType[] { ContractParameterType.Boolean }, new byte[] { 1 });
            WalletAccount account = wallet.CreateAccount(contract, glkey.PrivateKey);
            account.Lock = false;

            // Fake balance
            var snapshot = store.GetSnapshot();
            var key = NativeContract.LINK.CreateStorageKey(20, account.ScriptHash);
            var entry = snapshot.Storages.GetAndChange(key, () => new StorageItem
            {
                Value = new Tlp5AccountState().ToByteArray()
            });
            entry.Value = new Tlp5AccountState()
            {
                Balance = 10000 * NativeContract.LINK.Factor
            }
            .ToByteArray();

            wallet.GetBalance(UInt160.Zero, new UInt160[] { account.ScriptHash }).Should().Be(new BigDecimal(0, 0));
            wallet.GetBalance(NativeContract.LINK.Hash, new UInt160[] { account.ScriptHash }).Should().Be(new BigDecimal(1000000000000, 8));

            entry.Value = new Tlp5AccountState()
            {
                Balance = 0
            }
            .ToByteArray();
        }

        [TestMethod]
        public void TestGetPrivateKeyFromNEP2()
        {
            Action action = () => Wallet.GetPrivateKeyFromTLP2(null, null, 0, 0, 0);
            action.ShouldThrow<ArgumentNullException>();

            action = () => Wallet.GetPrivateKeyFromTLP2("TestGetPrivateKeyFromNEP2", null, 0, 0, 0);
            action.ShouldThrow<ArgumentNullException>();

            action = () => Wallet.GetPrivateKeyFromTLP2("3vQB7B6MrGQZaxCuFg4oh", "TestGetPrivateKeyFromNEP2", 0, 0, 0);
            action.ShouldThrow<FormatException>();

            action = () => Wallet.GetPrivateKeyFromTLP2(nep2Key, "Test", 0, 0, 0);
            action.ShouldThrow<FormatException>();

            Wallet.GetPrivateKeyFromTLP2(nep2Key, "pwd", 0, 0, 0).Should().BeEquivalentTo(new byte[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31 });
        }

        [TestMethod]
        public void TestGetPrivateKeyFromWIF()
        {
            Action action = () => Wallet.GetPrivateKeyFromWIF(null);
            action.ShouldThrow<ArgumentNullException>();

            action = () => Wallet.GetPrivateKeyFromWIF("3vQB7B6MrGQZaxCuFg4oh");
            action.ShouldThrow<FormatException>();

            Wallet.GetPrivateKeyFromWIF("L3tgppXLgdaeqSGSFw1Go3skBiy8vQAM7YMXvTHsKQtE16PBncSU").Should().BeEquivalentTo(new byte[] { 199, 19, 77, 111, 216, 231, 61, 129, 158, 130, 117, 92, 100, 201, 55, 136, 216, 219, 9, 97, 146, 158, 2, 90, 83, 54, 60, 76, 192, 42, 105, 98 });
        }

        [TestMethod]
        public void TestImport1()
        {
            MyWallet wallet = new MyWallet();
            wallet.Import("L3tgppXLgdaeqSGSFw1Go3skBiy8vQAM7YMXvTHsKQtE16PBncSU").Should().NotBeNull();
        }

        [TestMethod]
        public void TestImport2()
        {
            MyWallet wallet = new MyWallet();
            wallet.Import(nep2Key, "pwd", 0, 0, 0).Should().NotBeNull();
        }

        [TestMethod]
        public void TestMakeTransaction1()
        {
            MyWallet wallet = new MyWallet();
            Contract contract = Contract.Create(new ContractParameterType[] { ContractParameterType.Boolean }, new byte[] { 1 });
            WalletAccount account = wallet.CreateAccount(contract, glkey.PrivateKey);
            account.Lock = false;

            Action action = () => wallet.MakeTransaction(new TransferOutput[]
            {
                new TransferOutput()
                {
                     AssetId = NativeContract.LINK.Hash,
                     ScriptHash = account.ScriptHash,
                     Value = new BigDecimal(1,8)
                }
            }, UInt160.Zero);
            action.ShouldThrow<ArgumentException>();

            action = () => wallet.MakeTransaction(new TransferOutput[]
            {
                new TransferOutput()
                {
                     AssetId = NativeContract.LINK.Hash,
                     ScriptHash = account.ScriptHash,
                     Value = new BigDecimal(1,8)
                }
            }, account.ScriptHash);
            action.ShouldThrow<InvalidOperationException>();

            action = () => wallet.MakeTransaction(new TransferOutput[]
            {
                new TransferOutput()
                {
                     AssetId = UInt160.Zero,
                     ScriptHash = account.ScriptHash,
                     Value = new BigDecimal(1,8)
                }
            }, account.ScriptHash);
            action.ShouldThrow<InvalidOperationException>();

            // Fake balance
            var snapshot = store.GetSnapshot();
            var key = NativeContract.LINK.CreateStorageKey(20, account.ScriptHash);
            var entry = snapshot.Storages.GetAndChange(key, () => new StorageItem
            {
                Value = new Tlp5AccountState().ToByteArray()
            });
            entry.Value = new Tlp5AccountState()
            {
                Balance = 10000 * NativeContract.LINK.Factor
            }
            .ToByteArray();

            key = NativeContract.TRUST.CreateStorageKey(20, account.ScriptHash);
            entry = snapshot.Storages.GetAndChange(key, () => new StorageItem
            {
                Value = new Tlp5AccountState().ToByteArray()
            });
            entry.Value = new TrustToken.AccountState()
            {
                Balance = 10000 * NativeContract.TRUST.Factor
            }
            .ToByteArray();

            var tx = wallet.MakeTransaction(new TransferOutput[]
            {
                new TransferOutput()
                {
                     AssetId = NativeContract.LINK.Hash,
                     ScriptHash = account.ScriptHash,
                     Value = new BigDecimal(1,8)
                }
            });
            tx.Should().NotBeNull();

            tx = wallet.MakeTransaction(new TransferOutput[]
            {
                new TransferOutput()
                {
                     AssetId = NativeContract.TRUST.Hash,
                     ScriptHash = account.ScriptHash,
                     Value = new BigDecimal(1,8)
                }
            });
            tx.Should().NotBeNull();

            entry.Value = new TrustToken.AccountState()
            {
                Balance = 0
            }
            .ToByteArray();
        }

        [TestMethod]
        public void TestMakeTransaction2()
        {
            MyWallet wallet = new MyWallet();
            Action action = () => wallet.MakeTransaction(new byte[] { }, UInt160.Zero, new TransactionAttribute[] { });
            action.ShouldThrow<ArgumentException>();

            Contract contract = Contract.Create(new ContractParameterType[] { ContractParameterType.Boolean }, new byte[] { 1 });
            WalletAccount account = wallet.CreateAccount(contract, glkey.PrivateKey);
            account.Lock = false;

            // Fake balance
            var snapshot = store.GetSnapshot();
            var key = NativeContract.LINK.CreateStorageKey(20, account.ScriptHash);
            var entry = snapshot.Storages.GetAndChange(key, () => new StorageItem
            {
                Value = new Tlp5AccountState().ToByteArray()
            });
            entry.Value = new Tlp5AccountState()
            {
                Balance = 1000000 * NativeContract.LINK.Factor
            }
            .ToByteArray();

            var tx = wallet.MakeTransaction(new byte[] { }, account.ScriptHash, new TransactionAttribute[] { });
            tx.Should().NotBeNull();

            tx = wallet.MakeTransaction(new byte[] { }, null, new TransactionAttribute[] { });
            tx.Should().NotBeNull();

            entry.Value = new TrustToken.AccountState()
            {
                Balance = 0
            }
            .ToByteArray();
        }

        [TestMethod]
        public void TestVerifyPassword()
        {
            MyWallet wallet = new MyWallet();
            Action action = () => wallet.VerifyPassword("Test");
            action.ShouldNotThrow();
        }
    }
}
