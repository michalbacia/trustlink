using System;
using Moq;
using Trustlink.IO.Wrappers;
using Trustlink.Ledger;
using Trustlink.Persistence;

namespace Trustlink.UnitTests
{
    public static class TestBlockchain
    {
        private static NeoSystem TheNeoSystem;
        private static Mock<Store> _Store;

        public static Store GetStore()
        {
            if (_Store == null) InitializeMockNeoSystem();
            return _Store.Object;
        }

        public static NeoSystem InitializeMockNeoSystem()
        {
            if (TheNeoSystem == null)
            {
                var mockSnapshot = new Mock<Snapshot>();
                mockSnapshot.SetupGet(p => p.Blocks).Returns(new TestDataCache<UInt256, TrimmedBlock>());
                mockSnapshot.SetupGet(p => p.Transactions).Returns(new TestDataCache<UInt256, TransactionState>());
                mockSnapshot.SetupGet(p => p.Contracts).Returns(new TestDataCache<UInt160, ContractState>());
                mockSnapshot.SetupGet(p => p.Storages).Returns(new TestDataCache<StorageKey, StorageItem>());
                mockSnapshot.SetupGet(p => p.HeaderHashList).Returns(new TestDataCache<UInt32Wrapper, HeaderHashList>());
                mockSnapshot.SetupGet(p => p.BlockHashIndex).Returns(new TestMetaDataCache<HashIndexState>());
                mockSnapshot.SetupGet(p => p.HeaderHashIndex).Returns(new TestMetaDataCache<HashIndexState>());

                _Store = new Mock<Store>();

                var defaultTx = TestUtils.CreateRandomHashTransaction();
                _Store.Setup(p => p.GetBlocks()).Returns(new TestDataCache<UInt256, TrimmedBlock>());
                _Store.Setup(p => p.GetTransactions()).Returns(new TestDataCache<UInt256, TransactionState>(
                    new TransactionState
                    {
                        BlockIndex = 1,
                        Transaction = defaultTx
                    }));

                _Store.Setup(p => p.GetContracts()).Returns(new TestDataCache<UInt160, ContractState>());
                _Store.Setup(p => p.GetStorages()).Returns(new TestDataCache<StorageKey, StorageItem>());
                _Store.Setup(p => p.GetHeaderHashList()).Returns(new TestDataCache<UInt32Wrapper, HeaderHashList>());
                _Store.Setup(p => p.GetBlockHashIndex()).Returns(new TestMetaDataCache<HashIndexState>());
                _Store.Setup(p => p.GetHeaderHashIndex()).Returns(new TestMetaDataCache<HashIndexState>());
                _Store.Setup(p => p.GetSnapshot()).Returns(mockSnapshot.Object);

                Console.WriteLine("initialize NeoSystem");
                TheNeoSystem = new NeoSystem(_Store.Object); // new Mock<NeoSystem>(mockStore.Object);

                // Ensure that blockchain is loaded

                var blockchain = Blockchain.Singleton;
            }

            return TheNeoSystem;
        }
    }
}
