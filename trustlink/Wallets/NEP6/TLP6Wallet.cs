using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Trustlink.IO.Json;
using Trustlink.SmartContract;
using UserWallet = Trustlink.Wallets.SQLite.UserWallet;

namespace Trustlink.Wallets.NEP6
{
    // ReSharper disable once InconsistentNaming
    public class TLP6Wallet: Wallet
    {
        private readonly string path;
        private string password;
        private string name;
        private Version version;
        private readonly Dictionary<UInt160, TLP6Account> accounts;
        private readonly JObject extra;

        public readonly ScryptParameters Scrypt;
        public override string Name => name;
        public override Version Version => version;

        public TLP6Wallet(string path, string name = null)
        {
            this.path = path;
            if (File.Exists(path))
            {
                JObject wallet;
                using (StreamReader reader = new StreamReader(path))
                {
                    wallet = JObject.Parse(reader);
                }
                LoadFromJson(wallet, out Scrypt, out accounts, out extra);
            }
            else
            {
                this.name = name;
                this.version = Version.Parse("1.0");
                this.Scrypt = ScryptParameters.Default;
                this.accounts = new Dictionary<UInt160, TLP6Account>();
                this.extra = JObject.Null;
            }
        }

        public TLP6Wallet(JObject wallet)
        {
            this.path = "";
            LoadFromJson(wallet, out Scrypt, out accounts, out extra);
        }

        private void LoadFromJson(JObject wallet, out ScryptParameters scrypt, out Dictionary<UInt160, TLP6Account> accounts, out JObject extra)
        {
            this.name = wallet["name"]?.AsString();
            this.version = Version.Parse(wallet["version"].AsString());
            scrypt = ScryptParameters.FromJson(wallet["scrypt"]);
            accounts = ((JArray)wallet["accounts"]).Select(p => TLP6Account.FromJson(p, this)).ToDictionary(p => p.ScriptHash);
            extra = wallet["extra"];
        }

        private void AddAccount(TLP6Account account, bool is_import)
        {
            lock (accounts)
            {
                if (accounts.TryGetValue(account.ScriptHash, out TLP6Account account_old))
                {
                    account.Label = account_old.Label;
                    account.IsDefault = account_old.IsDefault;
                    account.Lock = account_old.Lock;
                    if (account.Contract == null)
                    {
                        account.Contract = account_old.Contract;
                    }
                    else
                    {
                        TLP6Contract contract_old = (TLP6Contract)account_old.Contract;
                        if (contract_old != null)
                        {
                            TLP6Contract contract = (TLP6Contract)account.Contract;
                            contract.ParameterNames = contract_old.ParameterNames;
                            contract.Deployed = contract_old.Deployed;
                        }
                    }
                    account.Extra = account_old.Extra;
                }
                accounts[account.ScriptHash] = account;
            }
        }

        public override bool Contains(UInt160 scriptHash)
        {
            lock (accounts)
            {
                return accounts.ContainsKey(scriptHash);
            }
        }

        public override WalletAccount CreateAccount(byte[] privateKey)
        {
            KeyPair key = new KeyPair(privateKey);
            TLP6Contract contract = new TLP6Contract
            {
                Script = Contract.CreateSignatureRedeemScript(key.PublicKey),
                ParameterList = new[] { ContractParameterType.Signature },
                ParameterNames = new[] { "signature" },
                Deployed = false
            };
            TLP6Account account = new TLP6Account(this, contract.ScriptHash, key, password)
            {
                Contract = contract
            };
            AddAccount(account, false);
            return account;
        }

        public override WalletAccount CreateAccount(Contract contract, KeyPair key = null)
        {
            TLP6Contract tlp6contract = contract as TLP6Contract;
            if (tlp6contract == null)
            {
                tlp6contract = new TLP6Contract
                {
                    Script = contract.Script,
                    ParameterList = contract.ParameterList,
                    ParameterNames = contract.ParameterList.Select((p, i) => $"parameter{i}").ToArray(),
                    Deployed = false
                };
            }
            TLP6Account account;
            if (key == null)
                account = new TLP6Account(this, tlp6contract.ScriptHash);
            else
                account = new TLP6Account(this, tlp6contract.ScriptHash, key, password);
            account.Contract = tlp6contract;
            AddAccount(account, false);
            return account;
        }

        public override WalletAccount CreateAccount(UInt160 scriptHash)
        {
            TLP6Account account = new TLP6Account(this, scriptHash);
            AddAccount(account, true);
            return account;
        }

        public KeyPair DecryptKey(string tlp2key)
        {
            return new KeyPair(GetPrivateKeyFromNEP2(tlp2key, password, Scrypt.N, Scrypt.R, Scrypt.P));
        }

        public override bool DeleteAccount(UInt160 scriptHash)
        {
            lock (accounts)
            {
                return accounts.Remove(scriptHash);
            }
        }

        public override WalletAccount GetAccount(UInt160 scriptHash)
        {
            lock (accounts)
            {
                accounts.TryGetValue(scriptHash, out NEP6Account account);
                return account;
            }
        }

        public override IEnumerable<WalletAccount> GetAccounts()
        {
            lock (accounts)
            {
                foreach (NEP6Account account in accounts.Values)
                    yield return account;
            }
        }

        public override WalletAccount Import(X509Certificate2 cert)
        {
            KeyPair key;
            using (ECDsa ecdsa = cert.GetECDsaPrivateKey())
            {
                key = new KeyPair(ecdsa.ExportParameters(true).D);
            }
            NEP6Contract contract = new NEP6Contract
            {
                Script = Contract.CreateSignatureRedeemScript(key.PublicKey),
                ParameterList = new[] { ContractParameterType.Signature },
                ParameterNames = new[] { "signature" },
                Deployed = false
            };
            NEP6Account account = new NEP6Account(this, contract.ScriptHash, key, password)
            {
                Contract = contract
            };
            AddAccount(account, true);
            return account;
        }

        public override WalletAccount Import(string wif)
        {
            KeyPair key = new KeyPair(GetPrivateKeyFromWIF(wif));
            NEP6Contract contract = new NEP6Contract
            {
                Script = Contract.CreateSignatureRedeemScript(key.PublicKey),
                ParameterList = new[] { ContractParameterType.Signature },
                ParameterNames = new[] { "signature" },
                Deployed = false
            };
            NEP6Account account = new NEP6Account(this, contract.ScriptHash, key, password)
            {
                Contract = contract
            };
            AddAccount(account, true);
            return account;
        }

        public override WalletAccount Import(string tlp2, string passphrase, int N = 16384, int r = 8, int p = 8)
        {
            KeyPair key = new KeyPair(GetPrivateKeyFromNEP2(tlp2, passphrase, N, r, p));
            NEP6Contract contract = new NEP6Contract
            {
                Script = Contract.CreateSignatureRedeemScript(key.PublicKey),
                ParameterList = new[] { ContractParameterType.Signature },
                ParameterNames = new[] { "signature" },
                Deployed = false
            };
            NEP6Account account;
            if (Scrypt.N == 16384 && Scrypt.R == 8 && Scrypt.P == 8)
                account = new NEP6Account(this, contract.ScriptHash, tlp2);
            else
                account = new NEP6Account(this, contract.ScriptHash, key, passphrase);
            account.Contract = contract;
            AddAccount(account, true);
            return account;
        }

        internal void Lock()
        {
            password = null;
        }

        public static TLP6Wallet Migrate(string path, string db3path, string password)
        {
            UserWallet wallet_old = UserWallet.Open(db3path, password);
            TLP6Wallet wallet_new = new TLP6Wallet(path, wallet_old.Name);
            using (wallet_new.Unlock(password))
            {
                foreach (WalletAccount account in wallet_old.GetAccounts())
                {
                    wallet_new.CreateAccount(account.Contract, account.GetKey());
                }
            }
            return wallet_new;
        }

        public void Save()
        {
            JObject wallet = new JObject();
            wallet["name"] = name;
            wallet["version"] = version.ToString();
            wallet["scrypt"] = Scrypt.ToJson();
            wallet["accounts"] = new JArray(accounts.Values.Select(p => p.ToJson()));
            wallet["extra"] = extra;
            File.WriteAllText(path, wallet.ToString());
        }

        public IDisposable Unlock(string password)
        {
            if (!VerifyPassword(password))
                throw new CryptographicException();
            this.password = password;
            return new WalletLocker(this);
        }

        public override bool VerifyPassword(string password)
        {
            lock (accounts)
            {
                NEP6Account account = accounts.Values.FirstOrDefault(p => !p.Decrypted);
                if (account == null)
                {
                    account = accounts.Values.FirstOrDefault(p => p.HasKey);
                }
                if (account == null) return true;
                if (account.Decrypted)
                {
                    return account.VerifyPassword(password);
                }
                else
                {
                    try
                    {
                        account.GetKey(password);
                        return true;
                    }
                    catch (FormatException)
                    {
                        return false;
                    }
                }
            }
        }
    }
}
