using System.Collections.Generic;
using Trustlink.Network.P2P.Payloads;

namespace Trustlink.Plugins
{
    public interface IMemoryPoolTxObserverPlugin
    {
        void TransactionAdded(Transaction tx);
        void TransactionsRemoved(MemoryPoolTxRemovalReason reason, IEnumerable<Transaction> transactions);
    }
}
