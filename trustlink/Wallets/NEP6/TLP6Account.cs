using System;
using Trustlink.IO.Json;

namespace Trustlink.Wallets.NEP6
{
    internal class TLP6Account : WalletAccount
    {
        private readonly TLP6Wallet wallet;
        private readonly string nep2key;
        private KeyPair key;
        public JObject Extra;

        public bool Decrypted => nep2key == null || key != null;
        public override bool HasKey => nep2key != null;

        public TLP6Account(TLP6Wallet wallet, UInt160 scriptHash, string nep2key = null)
            : base(scriptHash)
        {
            this.wallet = wallet;
            this.nep2key = nep2key;
        }

        public TLP6Account(TLP6Wallet wallet, UInt160 scriptHash, KeyPair key, string password)
            : this(wallet, scriptHash, key.Export(password, wallet.Scrypt.N, wallet.Scrypt.R, wallet.Scrypt.P))
        {
            this.key = key;
        }

        public static TLP6Account FromJson(JObject json, TLP6Wallet wallet)
        {
            return new TLP6Account(wallet, json["address"].AsString().ToScriptHash(), json["key"]?.AsString())
            {
                Label = json["label"]?.AsString(),
                IsDefault = json["isDefault"].AsBoolean(),
                Lock = json["lock"].AsBoolean(),
                Contract = TLP6Contract.FromJson(json["contract"]),
                Extra = json["extra"]
            };
        }

        public override KeyPair GetKey()
        {
            if (nep2key == null) return null;
            if (key == null)
            {
                key = wallet.DecryptKey(nep2key);
            }
            return key;
        }

        public KeyPair GetKey(string password)
        {
            if (nep2key == null) return null;
            if (key == null)
            {
                key = new KeyPair(Wallet.GetPrivateKeyFromNEP2(nep2key, password, wallet.Scrypt.N, wallet.Scrypt.R, wallet.Scrypt.P));
            }
            return key;
        }

        public JObject ToJson()
        {
            JObject account = new JObject();
            account["address"] = ScriptHash.ToAddress();
            account["label"] = Label;
            account["isDefault"] = IsDefault;
            account["lock"] = Lock;
            account["key"] = nep2key;
            account["contract"] = ((NEP6Contract)Contract)?.ToJson();
            account["extra"] = Extra;
            return account;
        }

        public bool VerifyPassword(string password)
        {
            try
            {
                Wallet.GetPrivateKeyFromNEP2(nep2key, password, wallet.Scrypt.N, wallet.Scrypt.R, wallet.Scrypt.P);
                return true;
            }
            catch (FormatException)
            {
                return false;
            }
        }
    }
}
