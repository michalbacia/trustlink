using Trustlink.IO.Caching;
using Trustlink.IO.Wrappers;
using Trustlink.Ledger;

namespace Trustlink.Persistence
{
    public interface IPersistence
    {
        DataCache<UInt256, TrimmedBlock> Blocks { get; }
        DataCache<UInt256, TransactionState> Transactions { get; }
        DataCache<UInt160, ContractState> Contracts { get; }
        DataCache<StorageKey, StorageItem> Storages { get; }
        DataCache<UInt32Wrapper, HeaderHashList> HeaderHashList { get; }
        MetaDataCache<HashIndexState> BlockHashIndex { get; }
        MetaDataCache<HashIndexState> HeaderHashIndex { get; }
    }
}
