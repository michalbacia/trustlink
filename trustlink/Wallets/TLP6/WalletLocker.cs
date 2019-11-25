using System;

namespace Trustlink.Wallets.TLP6
{
    internal class WalletLocker : IDisposable
    {
        private TLP6Wallet wallet;

        public WalletLocker(TLP6Wallet wallet)
        {
            this.wallet = wallet;
        }

        public void Dispose()
        {
            wallet.Lock();
        }
    }
}
